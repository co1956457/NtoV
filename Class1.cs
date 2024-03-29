// http://c-loft.com/blog/?p=719     この記事を参考に作成                              webの公開情報
// https://github.com/chinng-inta    運営コメントの条件分岐等参考にした                MIT License
// https://github.com/oocytanb       CommentBaton から縦書きコメビュにメッセージを送る MIT License
//                                   /cytanb-vci-comment-plugin 内 cytanb-comment-source v0.9.2 の送信部分をモジュール実装
//
// SPDX-License-Identifier: MIT
// 20200718 v1.0 Taki co1956457
// 20200725 v2.0 タイマー方式に変更
//               cytanb を最新版に更新 (ver. Commits on Jul 24, 2020)
// 20200728 v2.1 commentSource を NCV に変更
//               接続時に運営コメントが2回流れないよう対策
//               特殊文字対策
// 20201003 v2.2 cytanbをモジュール化 (ver. Commits on Sep 29, 2020)
// 20201003 v2.3 設定ファイルの改行対応
// 20201004 v2.4 local cytanb -> cytanb
// 20210218 v3.0 転送モード Transfer mode
// 20210301 v3.1 7000 -> 8000ms
// 20220605 v4.0 1フレーム1コメント方式, 広告コメント修正, ギフトコメント修正

using System;
using System.IO;                    // File, Directory
using System.Collections.Generic;   // List
using System.Windows.Forms;         // MessageBox
using System.Timers;                // Timer
using System.Linq;                  // Last
using System.Text;                  // StringBuilder

using Plugin;

namespace NtoV
{
    public class Class1 : IPlugin
    {
        private IPluginHost _host = null;

        #region IPlugin メンバ

        // Form用
        private Form1 _form1;

        // ファイル存在確認エラー用
        int fileExist;

        // プラグインの状態
        // transferMode
        //  0: 転送しない OFF
        //  1: スタジオ STUDIO [運営コメント Special comments]
        //  2: ルーム ROOM [全転送 All comments]
        public int transferMode;

        // 起動時にだけファイルから転送モードを読み込む
        private int initialRead = 0;

        // カレントディレクトリ = プラグインディレクトリ（AppData\Roaming）
        string curDirectory = Environment.CurrentDirectory;
        // CommentBaton のディレクトリ用
        string targetDirectory;
        // CommentBaton のパス
        string targetPath;
        // 設定ファイル のパス
        string readPath;

        // ID List
        string[] idList = new string[] {};
        // name List
        string[] nameList = new string[] {};

        // 送信コメントをためておく
        List<string> emitCmnt = new List<string>();
        List<string> buffEmit = new List<string>();

        // 最初の接続判定用
        int connected = 0;

        // タイマーの生成
        System.Timers.Timer timer = new System.Timers.Timer();

        /// <summary>
        /// プラグインの名前
        /// </summary>
        public string Name
        {
            // get { throw new NotImplementedException(); }
            get { return "NtoV 設定(Settings)"; }
        }

        /// <summary>
        /// プラグインのバージョン
        /// </summary>
        public string Version
        {
            // get { throw new NotImplementedException(); }
            get { return "4.0"; }
        }

        /// <summary>
        /// プラグインの説明
        /// </summary>
        public string Description
        {
            // get { throw new NotImplementedException(); }
            get { return "NCVからVirtualCastへコメント転送"; }
        }

        /// <summary>
        /// プラグインのホスト
        /// </summary>
        public IPluginHost Host
        {
            get
            {
                // throw new NotImplementedException();
                return this._host;
            }
            set
            {
                // throw new NotImplementedException();
                this._host = value;
            }
        }

        /// <summary>
        /// アプリケーション起動時にプラグインを自動実行するかどうか
        /// </summary>
        public bool IsAutoRun
        {
            // get { throw new NotImplementedException(); }
            get { return true; }
        }

        /// <summary>
        /// IsAutoRunがtrueの場合、アプリケーション起動時に自動実行される
        /// </summary>
        public void AutoRun()
        {
            // ファイルの存在確認
            fileExist = fileExistError();


            if (fileExist == 0) // 問題なし
            {
                readXML();
                initialRead = 1;

                if (transferMode > 0) // 前回の設定:コメント転送ON
                {
                    // main.lua 初期化
                    File.WriteAllText(targetPath, "");

                    // タイマーの設定
                    // コメントがないときに一時停止する方法は保留
                    timer.Elapsed += new ElapsedEventHandler(OnElapsed_TimersTimer);
                    timer.Interval = 8000;

                    // タイマー開始
                    timer.Start();
                }
            }
            else // 問題あり
            {
                showFileExistError(fileExist);
            }

            // コメント受信時のイベントハンドラ追加
            _host.ReceivedComment += new ReceivedCommentEventHandler(_host_ReceivedComment);

            // 放送接続イベントハンドラ追加
            _host.BroadcastConnected += new BroadcastConnectedEventHandler(_host_BroadcastConnected);

            // 放送切断イベントハンドラ追加
            _host.BroadcastDisConnected += new BroadcastDisConnectedEventHandler(_host_BroadcastDisConnected);

            // 終了時イベントハンドラ追加
            _host.MainForm.FormClosing += MainForm_FormClosing;
        }

        /// <summary>
        /// プラグイン→ NtoV 設定(Settings) を選んだ時
        /// </summary>
        public void Run()
        {
            //フォームの生成
            _form1 = new Form1(this);
            _form1.Text = "NtoV 設定(Settings)";
            _form1.Show();
            _form1.FormClosed += new FormClosedEventHandler(_form1_FormClosed);
        }

        /// <summary>
        /// 閉じるボタン☒を押した時
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // ファイルの存在確認
            fileExist = fileExistError();
        
            if (fileExist == 0) // 問題なし
            {
                // main.lua 初期化
                File.WriteAllText(targetPath, "");
                // NtoV.txtに設定情報 transferMode を保存
                File.WriteAllText(readPath, targetDirectory + Environment.NewLine + transferMode);
            }
            else // 問題あり
            {
                // do nothing
            }
        }

        /// <summary>
        /// コメントを受信したら書き出すまでためておく
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _host_ReceivedComment(object sender, ReceivedCommentEventArgs e)
        {
            if(transferMode > 0) // 稼働中
            {
                // 受信したコメント数を取り出す
                int count = e.CommentDataList.Count;
                if (count == 0)
                {
                    return;
                }
                // 最新のコメントデータを取り出す
                NicoLibrary.NicoLiveData.LiveCommentData commentData = e.CommentDataList[count - 1];

                // コメント文字列を取り出す
                //commentData.Name は 空
                //string name = commentData.Name;　←/これで名前が取得できない
                //IDから取得する
                string comment = commentData.Comment;
                string userID = commentData.UserId;                

                if (transferMode == 0) // 転送しない
                { }
                else if (transferMode == 1) // スタジオ ニコ生運営コメのみ転送　一般コメ転送しない
                {
                    // 運営コメント判定
                    if (((commentData.PremiumBits & NicoLibrary.NicoLiveData.PremiumFlags.ServerComment) == NicoLibrary.NicoLiveData.PremiumFlags.ServerComment))
                    {
                        comment = editComment(comment);
                        // 追加
                        buffEmit.Add("        {value = \'" + comment + "\', sender = {name = \'" + "（運営）" + "\', type = \'comment\', commentSource = \'" + "NCV" + "\',},},");
                    }
                }
                else // 全転送 (transferMode ==2)
                {
                    // 運営コメント判定
                    if (((commentData.PremiumBits & NicoLibrary.NicoLiveData.PremiumFlags.ServerComment) == NicoLibrary.NicoLiveData.PremiumFlags.ServerComment))
                    {
                        // 運営コメント編集
                        comment = editComment(comment);
                        // 追加 運営の commentSource はNCV：CommentBatonを利用した既存VCIに影響が出るためこのまま。
                        buffEmit.Add("        {value = \'" + comment + "\', sender = {name = \'" + "（運営）" + "\', type = \'comment\', commentSource = \'" + "NCV" + "\',},},");
                    }
                    else // 一般コメント
                    {
                        // 追加
                        string name = nameFromXML(userID);
                        buffEmit.Add("        {value = \'" + comment + "\', sender = {name = \'" + name + "\', type = \'comment\', commentSource = \'" + "Nicolive" + "\',},},");
                    }
                }
            }
            else
            {
                // do nothing
            }
        }

        /// <summary>
        /// 放送接続時イベントハンドラ
        /// </summary>
        void _host_BroadcastConnected(object sender, EventArgs e)
        {
            if (transferMode > 0) // 稼働中
            {
                // ファイルの存在確認
                fileExist = fileExistError();
                if (fileExist == 0) // 問題ファイルなし
                {
                    // main.lua 初期化
                    File.WriteAllText(targetPath, "");

                    // 最初の接続判定用
                    connected = 0;
                }
                else // 問題あり
                {
                    showFileExistError(fileExist);
                }
            }
            else // プラグイン停止中
            {
                // do nothing
            }
        }

        /// <summary>
        /// 放送切断時イベントハンドラ
        /// </summary>
        void _host_BroadcastDisConnected(object sender, EventArgs e)
        {
            if (transferMode > 0) // 稼働中
            {
                // ファイルの存在確認
                fileExist = fileExistError();
                if (fileExist == 0) // 問題ファイルなし
                {
                    // main.lua 初期化
                    File.WriteAllText(targetPath, "");
                }
                else // 問題あり
                {
                    // 切断時エラーメッセージ不要
                    // showFileExistError(fileExist);
                }
            }
            else // プラグイン停止中
            {
                // do nothing
            }
        }

        /// <summary>
        /// コメントがあれば指定時間ごとに main.lua に書き出す
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnElapsed_TimersTimer(object sender, ElapsedEventArgs e)
        {
            emitCmnt = new List<string>(buffEmit);
            buffEmit.Clear();

            // 新しいコメントがあれば main.lua 上書き
            if (emitCmnt[0] != "")
            {
                string s1;
                string s2;
                string s3;

                // cytanb = cytanb or require('cytanb')(_ENV)は IsMine の中(ゲストの負荷軽減)。cytanb.EmitCommentMessage は update の中(1フレーム1コメント)。
                // \ -> \\      ' -> \'     " -> \"
                s1 = "local num = 1\nlocal commentList = {}\n\nif vci.assets.IsMine then\n    cytanb = cytanb or require('cytanb')(_ENV)\n    commentList = {\n";

                // 接続時は最新データーを１件
                // それ以外はたまっていたもの全部
                if (connected == 0)
                {
                    s2 = emitCmnt.Last();
                    connected = 1;
                }
                else
                {
                    s2 = string.Join("\n", emitCmnt);
                }
                emitCmnt.Clear();

                // 念のため最後に \n を入れておく
                // タイミングによっては Visual Studio Code で警告が出る場合がある？　よくわからない
                // なくても正常に動作はする
                s3 = "\n    }\nend\n\nfunction update()\n    if num <= #commentList then\n        local entry = commentList[num]\n        cytanb.EmitCommentMessage(entry.value, entry.sender)\n        num = num + 1\n    end\nend\n";

                File.WriteAllText(targetPath, s1 + s2 + s3);
            }
            else
            {
                // do nothing
            }
        }

        /// <summary>
        /// ファイルの存在確認
        /// </summary>
        int fileExistError()
        {
            // 値を返す用
            int returnInt;
            // 設定ファイル名
            readPath = curDirectory + "\\NtoV.txt";
            // main.lua
            string targetName;
            targetName = "\\main.lua";

            // ファイルの存在確認
            if (File.Exists(readPath)) // 設定ファイルあり
            {
                // 行ごとのに、テキストファイルの中身をすべて読み込む
                string[] lines = File.ReadAllLines(readPath);

                // 最後に終了コード999を追記 転送モード初設定判定用
                string[] settingLines = new string[lines.Length + 1];
                Array.Copy(lines, settingLines, lines.Length);
                settingLines[lines.Length] = "999";

                if (initialRead == 0) // 起動時のみファイルから転送モード読み込み
                {
                    // transferMode
                    //  0: 転送しない OFF
                    //  1: スタジオ STUDIO [運営コメント Special comments]
                    //  2: ルーム ROOM [全転送 All comments]
                    //
                    if (settingLines[1] == "0" || settingLines[1] == "1" || settingLines[1] == "2")
                    {
                        transferMode = int.Parse(settingLines[1]);
                    }
                    else
                    {
                        transferMode = 1; // initial setting
                    }
                }
                // ディレクトリ確認
                targetDirectory = lines[0];
                targetDirectory = targetDirectory.Replace("\r", "").Replace("\n", "");　// 設定ファイルの改行を削除
                string[] strF = targetDirectory.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                if (strF[strF.Length - 1] == "CommentBaton") // フォルダ名が CommentBaton
                {
                    if (Directory.Exists(targetDirectory) == false)
                    {
                        returnInt = 3; // 設定ファイルあり　名前は CommentBaton　指定ディレクトリなし
                    }
                    else // 設定ファイルあり　名前は CommentBaton　指定ディレクトリあり
                    {
                        // main.lua 存在確認
                        targetPath = targetDirectory + targetName;
                        if (File.Exists(targetPath) == false) // なかったら
                        {
                            // main.lua 作成
                            File.WriteAllText(targetPath, "");
                        }
                        returnInt = 0; // 問題なし
                    }
                }
                else
                {
                    returnInt = 2; // 設定ファイルあり 名前が違う
                }
            }
            else
            {
                returnInt = 1; // 設定ファイルなし
            }
            return returnInt;
        }

        /// <summary>
        /// エラー表示
        /// </summary>
        void showFileExistError(int errorNumber)
        {
            if (errorNumber == 1)
            {
                // プラグイン停止
                transferMode = 0;
                // タイマー停止
                timer.Stop();
                MessageBox.Show("プラグインを停止しました。\nThis plugin was stopped\n\n設定ファイルがありません。\nThere is no setting file.\n\n1. C:\\Users\\%ユーザー名%\\AppData\\Roaming\\posite-c\\NiconamaCommentViewer\\NtoV.txt を作成してください(※NCVのインストール先を変えた人は自分の環境に合わせてください)。\n   Please create the text file (the directory depends on your install directory).\n\n2. NtoV.txt に CommentBaton VCI の場所 C:\\Users\\%ユーザー名%\\AppData\\LocalLow\\infiniteloop Co,Ltd\\VirtualCast\\EmbeddedScriptWorkspace\\CommentBaton を書いてください。\n   Please write the CommentBaton VCI directory in the text file.\n\n3. NCVを立ち上げなおしてください。\n   Please reboot NCV.", "NtoV エラー error");
            }
            else if (errorNumber == 2)
            {
                // プラグイン停止
                transferMode = 0;
                // タイマー停止
                timer.Stop();
                MessageBox.Show("プラグインを停止しました。\nThis plugin was stopped\n\n指定ディレクトリが CommentBaton ではありません。\nThe directory is not CommentBaton.\n\n1. NtoV.txt の内容（ CommentBaton VCI の場所 C:\\Users\\%ユーザー名%\\AppData\\LocalLow\\infiniteloop Co,Ltd\\VirtualCast\\EmbeddedScriptWorkspace\\CommentBaton ）を確認してください。\n   Please check the CommentBaton directory in the NtoV.txt.\n\n2. NCVを立ち上げなおしてください。\n   Please reboot NCV.", "NtoV エラー error");
            }
            else if (errorNumber == 3)
            {
                // プラグイン停止
                transferMode = 0;
                // タイマー停止
                timer.Stop();
                MessageBox.Show("プラグインを停止しました。\nThis plugin was stopped\n\n指定ディレクトリがありません。\nThe directory does not Exist.\n\n1. NtoV.txt の内容（ CommentBaton VCI の場所 C:\\Users\\%ユーザー名%\\AppData\\LocalLow\\infiniteloop Co,Ltd\\VirtualCast\\EmbeddedScriptWorkspace\\CommentBaton ）と実在を確認してください。\n   Please check the CommentBaton directory in the NtoV.txt and existence.\n\n2. NCVを立ち上げなおしてください。\n   Please reboot NCV.", "NtoV エラー error");
            }
        }

        //フォームが閉じられた時のイベントハンドラ
        void _form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            int old_transferMode = transferMode;
            transferMode = _form1.tMode;

            if (old_transferMode > 0 && transferMode == 0)
            {
                // タイマー停止
                timer.Stop();
                // main.lua 初期化
                File.WriteAllText(targetPath, "");
            }
            else if (old_transferMode == 0 && transferMode > 0)
            {
                // タイマー開始
                timer.Start();
                // main.lua 初期化
                File.WriteAllText(targetPath, "");
            }

            if (old_transferMode != transferMode)
            {
                // 設定ファイルにパスとモードを保存
                File.WriteAllText(readPath, targetDirectory + Environment.NewLine + transferMode);
            }

            //フォームが閉じられた時のイベントハンドラ削除
            _form1.FormClosed -= _form1_FormClosed;
            _form1 = null;
        }


        /// <summary>
        /// NCVに保存されているIDと名前を読み込む
        /// </summary>
        void readXML()
        {
            // ユーザー名をファイルから取得
            string xmlPath = curDirectory + "\\UserSetting.xml";

            if (File.Exists(xmlPath) == false) // なかったら
            {
                // do nothing
            }
            else
            {
                // XML処理ができなかったので一般的なテキスト処理
                // 行ごとに、テキストファイルの中身をすべて読み込む
                // null回避かつサイズ同期
                // nameList = lines; にすると勝手にidListもnameListになる
                string[] lines = File.ReadAllLines(xmlPath);
                string[] nLines = File.ReadAllLines(xmlPath);
                idList = lines;
                nameList = nLines;

                // 3行目から最後-1行目まで IDと名前を取り出す
                // lines.Length - 2 にすると最後のID取得がうまくいかない
                for (int i = 2; i < lines.Length - 1; i++)
                {
                    string idLine = lines[i];
                    string nameLine = lines[i];
                    
                    int cut11 = idLine.LastIndexOf("\">");
                    string id1 = idLine.Remove(0, cut11 + 2);
                    int cut12 = id1.LastIndexOf("<");
                    string id2 = id1.Substring(0, cut12);
                    idList[i] = id2;

                    int cut21 = nameLine.IndexOf(" name=\"");
                    string name1 = nameLine.Remove(0, cut21 + 7);
                    int cut22 = name1.IndexOf("\" time=\"");
                    string name2 = name1.Substring(0, cut22);
                    nameList[i] = name2;
                }
            }
        }

        /// <summary>
        /// comment.UserID に対応する名前があれば返す
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        string nameFromXML(string userID)
        {
            string uID = userID;
            string userName;
            if (userID.All(char.IsDigit))
            {
                if ((idList != null) && (nameList != null))
                {
                    int num = Array.IndexOf(idList, uID);
                    if (num > -1) // IDがあれば
                    {
                        userName = nameList[num];
                    }
                    else // IDがない
                    {
                        // 一度だけリスト更新（名前の自動取得でファイルが更新されている可能性あり）
                        readXML();
                        if ((idList != null) && (nameList != null)) // 念のため
                        {
                            int num2 = Array.IndexOf(idList, uID);
                            if (num2 > -1)
                            {
                                userName = nameList[num2];
                            }
                            else // 名前の自動取得をしていない
                            {
                                userName = "（生ID）";
                            }
                        }
                        else// XMLからリストが作成できていない
                        {
                            userName = "（生ID）";
                        }
                    }
                }
                else // XMLからリストが作成できていない
                {
                    userName = "（生ID）";
                }
            }
            else // IDに数字以外のものが含まれる
            {
                userName = ""; // 184
            }
            return userName;
        }

        /// <summary>
        /// 運営コメントを編集
        /// </summary>
        string editComment(string message)
        {
            string msg = message.Replace("\"", "\\\"").Replace("\'", "\\\'");
            string[] str = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            switch (str[0])
            {
                case "/nicoad":
                    // ※フォーマットが変わった
                    // 旧 「\"」 前5削除 後5削除
                    // /nicoad {\"totalAdPoint\":12200,\"message\":\"Takiさんが600ptニコニ広告しました「おすすめの放送です」\",\"version\":\"1\"}
                    //
                    // 新 「"」で Split 後、文字列の最後の「\」削除
                    // /nicoad {\"version\":\"1\",\"totalAdPoint\":12200,\"message\":\"【広告貢献1位】Takiさんが100ptニコニ広告しました\"}
                    // /nicoad {\"version\":\"1\",\"totalAdPoint\":12200,\"message\":\"Takiさんが1000ptニコニ広告しました「おすすめの放送です」\"}
                    //

                    // 「"」でメッセージを分割 (ニコニコのニックネームには「"」「'」が使えない)
                    string[] nicoadCmnt = msg.Split(new char[] { '\"' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    // 広告メッセージ
                    string adMessage = nicoadCmnt[9];
                    
                    // 最後の文字「\」を削除
                    adMessage = adMessage.TrimEnd('\\');

                    // 特殊文字変換
                    adMessage = adMessage.Replace("\n", "").Replace("\r", "");
                    adMessage = adMessage.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
                    adMessage = adMessage.Replace("$", "＄").Replace("/", "／").Replace(",", "，");

                    msg = adMessage;
                    break;
                case "/info":
                    // 
                    // /info 1 市場に文字シールワッペン　ひらがな　紺　ふが登録されました。
                    // /info 2 1人がコミュニティに参加しました。
                    // /info 2 1人（プレミアム1人）がコミュニティをフォローしました。
                    // /info 3 30分延長しました
                    // /info 4 
                    // /info 5 
                    // /info 6 観測地域:ニコ県沿岸北部　震度:5弱　発生時間:2099年 7月 5日 07時 42分
                    // /info 7 震源地:ニコ県沖　震度:5弱　マグニチュード:5.8　発生時間:2099年 7月 5日 07時 42分
                    // /info 8 第1位にランクインしました
                    // /info 10 ニコニ広告枠から1人が来場しました
                    // /info 10 「雑談」が好きな1人が来場しました
                    // 
                    // 「"」なし
                    // /info 6,7 中に半角スペースあり
                    // 
                    msg = msg.Remove(0, 8); // 先頭10文字「/info * 」削除
                    break;
                case "/gift":
                    // 2****7 はニコニコの ID
                    // 通常ギフト、イベントギフト
                    // /gift seed 2****7 \"Taki\" 50 \"\" \"ひまわりの種\"
                    // /gift giftevent_niku 2****7 \"Taki\" 90 \"\" \"肉\"
                    // /gift giftevent_yasai 2****7 \"Taki\" 20 \"\" \"野菜\"
                    // /gift giftevent_mashumaro 2****7 \"Taki\" 10 \"\" \"焼きマシュマロ\"
                    // /gift giftevent_mashumaro NULL \"名無し\" 10 \"\" \"焼きマシュマロ\"
                    //
                    // Vギフトランキングあり
                    // /gift vcast_ocha 2****7 \"Taki\" 300 \"\" \"お茶\" 1
                    // 【ギフト貢献1位】Takiさんがギフト「お茶（300pt）」を贈りました
                    // 
                    // /gift vcast_free_shell 2****7 \"Taki\" 0 \"\" \"貝がら（６種ランダム）\" 2
                    // 【ギフト貢献2位】Takiさんがギフト「貝がら（6種ランダム）（0pt）」を贈りました
                    // 
                    // /gift vcast_free_shell NULL \"名無し\" 0 \"\" \"貝がら（6種ランダム）\"
                    // 名無しさんがギフト「貝がら（6種ランダム）（0pt）」を贈りました
                    // 
                    // 名前に半角スペースが入っている人がいる
                    // /gift vcast_ocha 2****7 \"Ta /,\ki(SPSLCY)\" 0 \"\" \"貝がら（6種ランダム）\" 1
                    //
                    // 先頭から1回目の「\"」+ 1 から
                    // 後ろから5回目の「\"」- 1 まで (「\" 」は 名無しNULL のときずれる)
                    // 名前に半角スペースが入っていても大丈夫
                    // 「"」「'」は全角に変換
                    // 
                    // string user = msg.Substring(fmNum, toNum); 落ちる
                    // 
                    // msg = /gift vcast_ocha 2****7 \"Taki\" 300 \"\" \"お茶\" 1
                    // msg = /gift vcast_ocha 2****7 \"Ta /,\ki(SPSLCY)\" 0 \"\" \"貝がら（6種ランダム）\" 1
                    // msg = /gift vcast_free_shell NULL \"名無し\" 0 \"\" \"貝がら（6種ランダム）\"
                    // 

                    // 「"」でメッセージを分割 (ニコニコのニックネームには「"」「'」が使えない)
                    string[] giftCmnt = msg.Split(new char[] { '\"' }, StringSplitOptions.RemoveEmptyEntries);

                    // ユーザー名
                    string user = giftCmnt[1];

                    // 最後の文字「\」を削除
                    user = user.TrimEnd('\\');

                    // 特殊文字変換
                    user = user.Replace("\n", "").Replace("\r", "");
                    user = user.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
                    user = user.Replace("$", "＄").Replace("/", "／").Replace(",", "，");


                    // ポイント
                    string pt = giftCmnt[2];

                    // 最後の文字「\」を削除後、前後の空白を削除
                    pt = pt.TrimEnd('\\');
                    pt = pt.Trim();


                    // ギフト名
                    string giftName = giftCmnt[5];

                    // 最後の文字「\」を削除
                    giftName = giftName.TrimEnd('\\');

                    // 特殊文字変換
                    giftName = giftName.Replace("\n", "").Replace("\r", "");
                    giftName = giftName.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
                    giftName = giftName.Replace("$", "＄").Replace("/", "／").Replace(",", "，");


                    // ランキング(Vギフト)
                    string rank = "";
                    if (giftCmnt.Length == 7)
                    {
                        // 位取得後、前後の空白を削除
                        rank = giftCmnt[6];
                        rank = rank.Trim();
                    }
 
                    if (rank == "")
                    {
                        // Takiさんがギフト「お茶（300pt）」を贈りました
                        // 名無しさんがギフト「お茶（300pt）」を贈りました
                        msg =  user + "さんがギフト「" + giftName + "（" + pt + "pt）」を贈りました";
                    }
                    else
                    {
                        //【ギフト貢献1位】Takiさんがギフト「お茶（300pt）」を贈りました
                        msg = "【ギフト貢献" + rank + "位】" + user + "さんがギフト「" + giftName + "（" + pt + "pt）」を贈りました";
                    }
                    // 各要素処理しているから msg.Replace() は不要
                    break;
                case "/spi":
                    // 
                    // /spi \"「みんなでつりっくま」がリクエストされました\"
                    // ※名前の中に半角スペースあり
                    // /spi \"「ミノダ ひらがな 大 ピンク ゆ P50I9255」がリクエストされました\"
                    // /spi \"「Line Race」がリクエストされました\"
                    // 
                    msg = msg.Remove(0, 7); // 先頭7文字「/spi \"」削除
                    msg = msg.TrimEnd('\"');
                    msg = msg.TrimEnd('\\');
                    break;
                case "/cruise":
                    // 
                    // /cruise \"まもなく生放送クルーズが到着します\"
                    // /cruise \"生放送クルーズが去っていきます\"
                    // 
                    msg = msg.Remove(0, 10); // 先頭10文字「/cruise \"」削除
                    msg = msg.TrimEnd('\"');
                    msg = msg.TrimEnd('\\');
                    break;
                case "/quote":
                    // 
                    // /quote \"「生放送クルーズさん」が引用を開始しました\"
                    // /quote \"ｗ（生放送クルーズさんの番組）\"
                    // /quote \"「生放送クルーズさん」が引用を終了しました\"
                    // 
                    msg = msg.Remove(0, 9); // 先頭9文字「/quote \"」削除
                    msg = msg.TrimEnd('\"');
                    msg = msg.TrimEnd('\\');
                    break;
                case "/uadpoint":
                    // 
                    // /uadpoint 123456789 6400   // 123456789 放送ID lvなし
                    // 
                    msg = "広告が設定されました累計ポイントが" + str[2] + "になりました";
                    msg = msg.Replace("\\\"", "");
                    break;
                 case "/perm":
                    //
                    // /perm <a href="https://example.com/example/2020071700999" target="_blank"><u>●商品No.1 「サンプル S999」</u></a>
                    // ●商品No.1 「サンプル S999」
                    //
                    // <u><font color="#00CCFF"><a href="https://www.nicovideo.jp/watch/sm36179129" class="video" target="_blank">sm36179129</a></font></u> BGM「よいしょ（Yoisho）」
                    // sm36179129 BGM「よいしょ（Yoisho）」
                    //
                    if (str[1] == "<a") // タグ <a> と <u> を外す
                    {
                        msg = "／perm　＜リンク＞　　　　" + removeA(msg);
                    }
                    else if (str[1] == "<u><font") // タグ <u> と <font> と <a> を外す removeA とは別
                    {
                        msg = "／perm　＜リンク＞　　　　" + removeUFA(msg);
                    }
                    else
                    {
                        // do nothing
                    }
                    break;
                 case "/vote":
                    //
                    // /vote start 質問文 選択肢1 選択肢2 選択肢3 選択肢4 選択肢5 選択肢6 選択肢7 選択肢8 選択肢9
                    // /vote start 質問文 選択肢1 選択肢2 選択肢3
                    //
                    // 半角スペースが入ると「"」で囲まれる   （質問文・選択肢2）
                    // 半角スペースがないものはそのまま      （選択肢1） 
                    // 「"」があると「\"」に変換される       （選択肢3）
                    //
                    // /vote start \"質 問 文\" 選択肢1 \"選 択 肢 2\" 選\\"択肢3
                    //
                    // \\"を全角”に変換
                    // \"を検索　もしあったら次の \" を検索
                    // \" 間の半角をアンダースコアに置換
                    // 質問文 + 選択肢最大9 回繰り返す
                    // \"削除　半角スペースで分割して str 上書き
                    //
                    msg = msg.Replace("\\\\\"", "”");
                    // 結果
                    // /vote start \"質 問 文\" 選択肢1 \"選 択 肢 2\" 選”択肢3

                    // \"を検索　もしあったら次の \" を検索
                    // \" 間の半角をアンダースコアに置換
                    // 質問文 + 選択肢最大9 回繰り返す
                    //
                    int fmDQ = msg.IndexOf("\"");
                    if (fmDQ != -1)
                    {
                        int toDQ = msg.IndexOf("\"", fmDQ + 1);
                        msg = spaceToUnderbar(msg, fmDQ, toDQ);

                        int fmDQ1 = msg.IndexOf("\"", toDQ + 1);
                        if (fmDQ1 != -1)
                        {
                            int toDQ1 = msg.IndexOf("\"", fmDQ1 + 1);
                            msg = spaceToUnderbar(msg, fmDQ1, toDQ1);

                            int fmDQ2 = msg.IndexOf("\"", toDQ1 + 1);
                            if (fmDQ2 != -1)
                            {
                                int toDQ2 = msg.IndexOf("\"", fmDQ2 + 1);
                                msg = spaceToUnderbar(msg, fmDQ2, toDQ2);

                                int fmDQ3 = msg.IndexOf("\"", toDQ2 + 1);
                                if (fmDQ3 != -1)
                                {
                                    int toDQ3 = msg.IndexOf("\"", fmDQ3 + 1);
                                    msg = spaceToUnderbar(msg, fmDQ3, toDQ3);

                                    int fmDQ4 = msg.IndexOf("\"", toDQ3 + 1);
                                    if (fmDQ4 != -1)
                                    {
                                        int toDQ4 = msg.IndexOf("\"", fmDQ4 + 1);
                                        msg = spaceToUnderbar(msg, fmDQ4, toDQ4);

                                        int fmDQ5 = msg.IndexOf("\"", toDQ4 + 1);
                                        if (fmDQ5 != -1)
                                        {
                                            int toDQ5 = msg.IndexOf("\"", fmDQ5 + 1);
                                            msg = spaceToUnderbar(msg, fmDQ5, toDQ5);

                                            int fmDQ6 = msg.IndexOf("\"", toDQ5 + 1);
                                            if (fmDQ6 != -1)
                                            {
                                                int toDQ6 = msg.IndexOf("\"", fmDQ6 + 1);
                                                msg = spaceToUnderbar(msg, fmDQ6, toDQ6);

                                                int fmDQ7 = msg.IndexOf("\"", toDQ6 + 1);
                                                if (fmDQ7 != -1)
                                                {
                                                    int toDQ7 = msg.IndexOf("\"", fmDQ7 + 1);
                                                    msg = spaceToUnderbar(msg, fmDQ7, toDQ7);

                                                    int fmDQ8 = msg.IndexOf("\"", toDQ7 + 1);
                                                    if (fmDQ8 != -1)
                                                    {
                                                        int toDQ8 = msg.IndexOf("\"", fmDQ8 + 1);
                                                        msg = spaceToUnderbar(msg, fmDQ8, toDQ8);

                                                        int fmDQ9 = msg.IndexOf("\"", toDQ8 + 1);
                                                        if (fmDQ9 != -1)
                                                        {
                                                            int toDQ9 = msg.IndexOf("\"", fmDQ9 + 1);
                                                            msg = spaceToUnderbar(msg, fmDQ9, toDQ9);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // 結果
                    // /vote start \"質_問_文\" 選択肢1 \"選_択_肢_2\" 選”択肢3

                    msg = msg.Replace("\\\"", "");
                    // 結果
                    // /vote start 質_問_文 選択肢1 選_択_肢_2 選”択肢3

                    // str 上書き
                    str = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    // /vote start 質_問_文 選択肢1 選_択_肢_2 選”択肢3
                    // 【アンケート開始】質_問_文
                    // 1：選択肢1
                    // 2：選_択_肢_2
                    // 3：選”択肢3
                    //
                    // 改行文字数調整不可能ではないが複雑すぎるので１行にする
                    // 【アンケート開始】質_問_文　1：選択肢1　2：選_択_肢_2　3：選”択肢3
                    //
                    // /vote showresult per 833 167 0
                    // 【アンケート結果】
                    // 1：83.3%　2：16.7%　3：0%
                    //
                    // /vote stop
                    // 【アンケート終了】
                    //
                    if (str[1] == "start")
                    {
                        msg = "【アンケート開始】" + str[2];

                        // 最大9つの選択肢の結果 
                        int len = str.Length;
                        for (int i = 3; i < len; i++)
                        {
                            if (str[i] != String.Empty)
                            {
                                int num = i - 2;
                                string orderNum = num.ToString();
                                msg = msg + "　" + orderNum + "：" + str[i];
                                // 結果
                                // msg = "【アンケート開始】質_問_文　1：選択肢1　2：選_択_肢_2　3：選”択肢3";
                            }
                            else
                            {
                                // do nothing str[i] = Empty
                            }
                        }
                    }
                    else if (str[1] == "showresult")
                    {
                        msg = "【アンケート結果】　　　　　"; // 改行の関係からここは14字
                        // 最大9つの選択肢の結果 
                        int len = str.Length;
                        for (int i = 3; i < len; i++)
                        {
                            if (str[i] != String.Empty)
                            {
                                int striLen = str[i].Length;
                                if (striLen == 1)
                                {
                                    str[i] = "0." + str[i]; // 6 -> 0.6
                                    int dotZero = str[i].IndexOf(".0");
                                    if (dotZero != -1)
                                    {
                                        str[i] = str[i].Remove(dotZero); // 0.0 -> 0
                                    }
                                }
                                else if (striLen == 2)
                                {
                                    str[i] = str[i].Insert(1, "."); // 83 -> 8.3
                                    // NCVでは「.0」が省略されるが画面上は表示される
                                    //int dotZero = str[i].IndexOf(".0");
                                    //if (dotZero != -1)
                                    //{
                                    //    str[i] = str[i].Remove(dotZero); // 5.0 -> 5
                                    //}
                                }
                                else if (striLen == 3)
                                {
                                    str[i] = str[i].Insert(2, "."); // 917 -> 91.7
                                    // NCVでは「.0」が省略されるが画面上は表示される
                                    //int dotZero = str[i].IndexOf(".0");
                                    //if (dotZero != -1)
                                    //{
                                    //    str[i] = str[i].Remove(dotZero); // 75.0 -> 75
                                    //}
                                }
                                else
                                {
                                    str[i] = "100"; // 1000 -> 100
                                }
                                int num = i - 2;
                                string orderNum = num.ToString();
                                msg = msg + "　" + orderNum + "：" + str[i] + "%";
                                // 結果
                                // msg = "【アンケート結果】　　　　　　1：91.7%　2：8.3%　3：0%";
                                // msg = "【アンケート結果】　　　　　　1：75.0%　2：20.0%　3：5.0%";
                            }
                            else
                            {
                                // do nothing str[i] = Empty
                            }
                        }
                    }
                    else if (str[1] == "stop")
                    {
                        msg = "【アンケート終了】";
                    }
                    else
                    {
                        // do nothing
                    }
                    break;
                case "<a":
                    //
                    // <a href="https://example.jp/example/example.html?from=live_watch_anime202099_player" target="_blank"><u>今期アニメ　niconicoでの配信一覧はこちら！｜Nアニメ</u></a>
                    // 今期アニメ　niconicoでの配信一覧はこちら！｜Nアニメ
                    //
                    // タグ <a> と <u> を外す
                    msg = "＜リンク＞" + removeA(msg);
                    break;
                case "<u><font":
                    //
                    // <u><font color="#00CCFF"><a href="https://www.nicovideo.jp/watch/sm36179129" class="video" target="_blank">sm36179129</a></font></u> てすと
                    // sm36179129 BGM「よいしょ（Yoisho）」
                    //
                    // タグ <u> と <font> と <a> を外す removeA とは別
                    msg = "＜リンク＞" + removeUFA(msg);
                    break;
                // 
                // デフォルト処理でいい
                // case "/disconnect":
                //    msg = "disconnect";
                //    msg = msg.Replace("\\\"", "");
                //    break;
                // 
                default:
                    // msg = msg.Replace("\n", "").Replace("\r", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    // msg = msg.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
                    break;
            }

            // 念のため
            if (msg == null)
            {
                msg = "（本文なし）";
            }

            // 改行コード等々
            msg = msg.Replace("\n", "").Replace("\r", "");
            msg = msg.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
            msg = msg.Replace("$", "＄").Replace("/", "／").Replace(",", "，");

            // 編集した運営コメントを 返す
            return msg;
        }
        
        /// <summary>
        /// タグ <a> と <u> を外す
        /// </summary>        
        string removeA(string msg)
        {
            //
            // /perm <a href="https://example.com/example/2020071700999" target="_blank"><u>●商品No.1 「サンプル S999」</u></a>
            // ●商品No.1 「サンプル S999」
            // 
            // <a href="https://example.jp/example/example.html?from=live_watch_anime202099_player" target="_blank"><u>今期アニメ　niconicoでの配信一覧はこちら！｜Nアニメ</u></a>
            // 今期アニメ　niconicoでの配信一覧はこちら！｜Nアニメ
            //
            // 先頭から <u> を検索 + 3 まで削除
            // 後ろに文字列がつく可能性あり　単に </u> </a> 削除
            //
            string linkName;
            int fmLinkName = msg.IndexOf("<u>");
            if (fmLinkName == -1) // <u> がなかったら
            {
                int fmLinkName1 = msg.IndexOf("_blank\\\">");
                if (fmLinkName1 == -1)
                {
                    // エラーが出るよりまし
                    linkName = msg;
                }
                else
                {
                    fmLinkName = fmLinkName1 + 9;
                    linkName = msg.Remove(0, fmLinkName);
                }
            }
            else
            {
                fmLinkName = fmLinkName + 3;
                linkName = msg.Remove(0, fmLinkName);
                // 結果
                // linkName = ●商品No.1 「サンプル S999」</u></a>
                // linkName = 今期アニメ　niconicoでの配信一覧はこちら！｜Nアニメ</u></a>
            }

            linkName = linkName.Replace("</a>", "").Replace("</font>", "").Replace("</u>", "");
            linkName = linkName.Replace("\n", "").Replace("\r", "");
            linkName = linkName.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
            linkName = linkName.Replace("$", "＄").Replace("/", "／").Replace(",", "，");
            // 結果
            // linkName = ●商品No.1 「サンプル S999」
            // linkName = 今期アニメ　niconicoでの配信一覧はこちら！｜Nアニメ

            msg = linkName;
            return msg;
        }

        /// <summary>
        /// タグ <u> と <font> と <a> を外す
        /// </summary>        
        string removeUFA(string msg)
        {
            //
            // /perm <u><font color=\"#00CCFF\"><a href=\"https://www.nicovideo.jp/watch/sm36179129\" class=\"video\" target=\"_blank\">sm36179129</a></font></u>
            // sm36179129
            // 
            // <u><font color=\"#00CCFF\"><a href=\"https://www.nicovideo.jp/watch/sm36179129\" class=\"video\" target=\"_blank\">sm36179129</a></font></u>
            // sm36179129
            //
            // <u><font color="#00CCFF"><a href="https://www.nicovideo.jp/watch/sm36179129" class="video" target="_blank">sm36179129</a></font></u> BGM「よいしょ（Yoisho）」
            // sm36179129 BGM「よいしょ（Yoisho）」
            //
            // lvから始まる放送番号も同じ形式
            //
            // 先頭から <_blank> を検索 + 8 まで削除
            // 後ろに文字列がつく可能性あり　単に </a> </font> </u> 削除
            //
            string linkName;
            int fmLinkName = msg.IndexOf("_blank\\\">");
            if (fmLinkName == -1) // _blank"> がなかったら
            {
                // エラーが出るよりまし
                linkName = msg;
            }
            else
            {
                fmLinkName = fmLinkName + 9;
                linkName = msg.Remove(0, fmLinkName);
                // 結果
                // linkName = sm36179129</a></font></u> BGM「よいしょ（Yoisho）」
            }

            linkName = linkName.Replace("</a>", "").Replace("</font>", "").Replace("</u>", "");
            linkName = linkName.Replace("\n", "").Replace("\r", "");
            linkName = linkName.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
            linkName = linkName.Replace("$", "＄").Replace("/", "／").Replace(",", "，");
            // 結果
            // linkName = sm36179129 BGM「よいしょ（Yoisho）」

            msg = linkName;
            return msg;
        }

        /// <summary>
        /// 範囲内で半角スペースを半角アンダースコアに置換
        /// </summary>        
        static string spaceToUnderbar(string msg, int from, int to)
        {
            // StringBuilderを作成する
            StringBuilder sb = new StringBuilder(msg);
            // from から to の範囲で、半角スペースを半角アンダースコアに置換する
            sb.Replace(" ", "_", from, to - from);
            // msgに戻す
            msg = sb.ToString();
            return msg;
        }
        #endregion
    }
}
