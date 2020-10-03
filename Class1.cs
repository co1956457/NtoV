// http://c-loft.com/blog/?p=719     この記事を参考に作成                              webの公開情報
// https://github.com/chinng-inta    運営コメントの条件分岐等参考にした                MIT License
// https://github.com/oocytanb       CommentBaton から縦書きコメビュにメッセージを送る MIT License
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
//
using System;
using System.IO;                    // File, Directory
using System.Collections.Generic;   // List
using System.Windows.Forms;         // MessageBox
using System.Text;                  // StringBuilder
using System.Timers;                // Timer
using System.Linq;                  // Last

using Plugin;

namespace NtoV

{
    public class Class1 : IPlugin
    {
        private IPluginHost _host = null;

        #region IPlugin メンバ

        // プラグインの起動・停止
        bool ONOFF = true;

        // ファイル存在確認エラー用
        int fileExist;

        // CommentBaton のパス
        string targetPath;

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
            get { return "NtoV [停止/開始]"; }
        }

        /// <summary>
        /// プラグインのバージョン
        /// </summary>
        public string Version
        {
            // get { throw new NotImplementedException(); }
            get { return "2.0"; }
        }

        /// <summary>
        /// プラグインの説明
        /// </summary>
        public string Description
        {
            // get { throw new NotImplementedException(); }
            get { return "CommentBaton VCI に運営コメを送る。"; }
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
            // プラグイン起動（念のため明示）
            ONOFF = true;

            // ファイルの存在確認
            fileExist = fileExistError();

            if (fileExist == 0) // 問題なし
            {
                // main.lua 初期化
                File.WriteAllText(targetPath, "");

                // タイマーの設定
                timer.Elapsed += new ElapsedEventHandler(OnElapsed_TimersTimer);
                timer.Interval = 8000;

                // タイマー開始
                timer.Start();
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
        }

        /// <summary>
        /// プラグイン→ NtoV を選んだ時
        /// </summary>
        public void Run()
        {
            // ファイルの存在確認
            fileExist = fileExistError();

            if (fileExist == 0) // 問題なし
            {
                // 稼働中なら停止　停止中なら開始
                if (ONOFF)
                {
                    // プラグイン停止
                    ONOFF = false;
                    // タイマー停止
                    timer.Stop();
                    // コメント受信時のイベントハンドラ削除
                    _host.ReceivedComment -= _host_ReceivedComment;
                    // メッセージ表示
                    MessageBox.Show("停止しました。\n\nStopped", Name);
                    // main.lua 初期化
                    File.WriteAllText(targetPath, "");
                }
                else
                {
                    // プラグイン開始
                    ONOFF = true;
                    // タイマー開始
                    timer.Start();
                    // コメント受信時のイベントハンドラ追加
                    _host.ReceivedComment += _host_ReceivedComment;
                    // メッセージ表示
                    MessageBox.Show("開始しました。\n\nStarted", Name);
                    // main.lua 初期化
                    File.WriteAllText(targetPath, "");
                }
            }
            else // 問題あり
            {
                showFileExistError(fileExist);
            }
        }

        /// <summary>
        /// 放送接続時イベントハンドラ
        /// </summary>
        void _host_BroadcastConnected(object sender, EventArgs e)
        {
            // プラグイン稼働中
            if (ONOFF)
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
            // プラグイン稼働中
            if (ONOFF)
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
        /// コメント受信時に呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _host_ReceivedComment(object sender, Plugin.ReceivedCommentEventArgs e)
        {
            if (ONOFF) // 稼働中
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
                string comment = commentData.Comment;

                // 運営コメントを編集する
                if (((commentData.PremiumBits & NicoLibrary.NicoLiveData.PremiumFlags.ServerComment) == NicoLibrary.NicoLiveData.PremiumFlags.ServerComment))
                {
                    comment = editComment(comment);
                    // 追加
                    buffEmit.Add("    cytanb.EmitCommentMessage(\'" + comment + "\', {name = \'" + "（運営）" + "\', commentSource = \'" + "NCV" + "\'})");
                }
            }
            else
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

                // cytanb.EmitCommentMessage の実行は IsMine の中に書くこと。そうしないとゲストの人数分実行されてしまう。
                // cytanb ver. Commits on Sep 29, 2020
                // \ -> \\      ' -> \'     " -> \"
                s1 = "cytanb = cytanb or require(\'cytanb\')(_ENV)\n\nif vci.assets.IsMine then\n";

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
                s3 = "\nend\n";

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
            // ファイルの存在確認
            int returnInt;
            string curDirectory = System.Environment.CurrentDirectory;
            string readPath = curDirectory + "\\NtoV.txt";
            string targetDirectory;
            string targetName;
            targetName = "\\main.lua";

            if (File.Exists(readPath)) // 設定ファイルあり
            {
                // ディレクトリ確認
                targetDirectory = File.ReadAllText(readPath);
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
                ONOFF = false;
                // タイマー停止
                timer.Stop();
                MessageBox.Show("プラグインを停止しました。\nThis plugin was stopped\n\n設定ファイルがありません。\nThere is no setting file.\n\n1. C:\\Users\\%ユーザー名%\\AppData\\Roaming\\posite-c\\NiconamaCommentViewer\\NtoV.txt を作成してください。\n   Please create the text file.\n\n2. NtoV.txt に CommentBaton VCI の場所 C:\\Users\\%ユーザー名%\\AppData\\LocalLow\\infiniteloop Co,Ltd\\VirtualCast\\EmbeddedScriptWorkspace\\CommentBaton を書いてください。\n   Please write the CommentBaton VCI directory in the text file.\n\n3. NCVを立ち上げなおしてください。\n   Please reboot NCV.", "NtoV エラー error");
            }
            else if (errorNumber == 2)
            {
                // プラグイン停止
                ONOFF = false;
                // タイマー停止
                timer.Stop();
                MessageBox.Show("プラグインを停止しました。\nThis plugin was stopped\n\n指定ディレクトリが CommentBaton ではありません。\nThe directory is not CommentBaton.\n\n1. NtoV.txt の内容（ CommentBaton VCI の場所 C:\\Users\\%ユーザー名%\\AppData\\LocalLow\\infiniteloop Co,Ltd\\VirtualCast\\EmbeddedScriptWorkspace\\CommentBaton ）を確認してください。\n   Please check the CommentBaton directory in the NtoV.txt.\n\n2. NCVを立ち上げなおしてください。\n   Please reboot NCV.", "NtoV エラー error");
            }
            else if (errorNumber == 3)
            {
                // プラグイン停止
                ONOFF = false;
                // タイマー停止
                timer.Stop();
                MessageBox.Show("プラグインを停止しました。\nThis plugin was stopped\n\n指定ディレクトリがありません。\nThe directory does not Exist.\n\n1. NtoV.txt の内容（ CommentBaton VCI の場所 C:\\Users\\%ユーザー名%\\AppData\\LocalLow\\infiniteloop Co,Ltd\\VirtualCast\\EmbeddedScriptWorkspace\\CommentBaton ）と実在を確認してください。\n   Please check the CommentBaton directory in the NtoV.txt and existence.\n\n2. NCVを立ち上げなおしてください。\n   Please reboot NCV.", "NtoV エラー error");
            }
        }

        /// <summary>
        /// 運営コメントを編集
        /// 参考 https://github.com/chinng-inta
        /// </summary>
        string editComment(string message)
        {
            string msg = message.Replace("\"", "\\\"").Replace("\'", "\\\'");
            string[] str = msg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            switch (str[0])
            {
                case "/nicoad":
                    //
                    // /nicoad {\"totalAdPoint\":12200,\"message\":\"Takiさんが600ptニコニ広告しました「おすすめの放送です」\",\"version\":\"1\"}
                    // 
                    // 名前に半角スペースが入っている人がいる
                    // ニコニコのニックネームには「"」「'」が使えない（他サイトは不明）
                    // ニコニコのニックネームを Ta /,\ki(SPSLCY)  に設定 (SPace SLash Comma Yenmark)
                    // /nicoad Json内では       Ta /,\\ki(SPSLCY) \ が \\ に（広告メッセージ中の \ も \\）
                    // /gift 内はそのまま       Ta /,\ki(SPSLCY)
                    //
                    // /nicoad {\"totalAdPoint\":12200,\"message\":\"Ta /,\\ki(SPSLCY)さんが600ptニコニ広告しました「\\ えんまーく」\",\"version\":\"1\"}
                    //
                    // str[1]には名前の半角スペースの前までしか入っていない
                    // 
                    // strJson = msg.Remove(0, 8); // 先頭8文字「/nicoad 」削除
                    // strJson = strJson.Replace("\\", "");
                    // 
                    // dynamic obj = DynamicJson.Parse(@"" + strJson);
                    // if (obj.IsDefined("message") == true)
                    // {
                    //    msg = obj.message;
                    // }
                    // else
                    // {
                    //    // mseesageLabel.Text = obj;
                    // }
                    //
                    // DynamicJson の扱いがわからなかった(参照とか)ので
                    // 先頭から5回目の「\"」 + 1 まで削除
                    // 後ろから5回目の「\"」 - 1 から最後まで削除
                    // 名前に半角スペースがあっても大丈夫
                    // 「"」「'」は全角に変換
                    // 
                    // msg.Substring(fmNum, toNum); なぜか落ちるから全部 remove で処理する
                    // 
                    // /nicoad {\"totalAdPoint\":12200,\"message\":\"Takiさんが600ptニコニ広告しました「おすすめの放送です」\",\"version\":\"1\"}
                    // /nicoad {\"totalAdPoint\":12200,\"message\":\"Ta /,\\ki(SPSLCY)さんが600ptニコニ広告しました「\\ えんまーく」\",\"version\":\"1\"}
                    // 
                    int fmAdMsg = msg.IndexOf("\"");
                    fmAdMsg = msg.IndexOf("\"", fmAdMsg + 1);
                    fmAdMsg = msg.IndexOf("\"", fmAdMsg + 1);
                    fmAdMsg = msg.IndexOf("\"", fmAdMsg + 1);
                    fmAdMsg = msg.IndexOf("\"", fmAdMsg + 1);
                    fmAdMsg = fmAdMsg + 1;
                    string adMessage = msg.Remove(0, fmAdMsg);
                    // 結果
                    // adMessage = Takiさんが600ptニコニ広告しました「おすすめの放送です」\",\"version\":\"1\"}
                    // adMessage = Ta /,\\ki(SPSLCY)さんが600ptニコニ広告しました「\\ えんまーく」\",\"version\":\"1\"}

                    int toAdMsg = adMessage.LastIndexOf("\"");
                    toAdMsg = adMessage.LastIndexOf("\"", toAdMsg - 1);
                    toAdMsg = adMessage.LastIndexOf("\"", toAdMsg - 1);
                    toAdMsg = adMessage.LastIndexOf("\"", toAdMsg - 1);
                    toAdMsg = adMessage.LastIndexOf("\"", toAdMsg - 1);
                    toAdMsg = toAdMsg - 1;
                    adMessage = adMessage.Remove(toAdMsg);
                    // 結果
                    // adMessage = Takiさんが600ptニコニ広告しました「おすすめの放送です」
                    // adMessage = Ta /,\\ki(SPSLCY)さんが600ptニコニ広告しました「\\ えんまーく」

                    adMessage = adMessage.Replace("\n", "").Replace("\r", "");
                    adMessage = adMessage.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
                    adMessage = adMessage.Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    // 結果
                    // adMessage = Takiさんが600ptニコニ広告しました「おすすめの放送です」
                    // adMessage = Ta ／，＼ki(SPSLCY)さんが300ptニコニ広告しました「＼　えんまーく」
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
                    // 
                    // 「"」なし
                    // /info 6,7 中に半角スペースあり
                    // 
                    msg = msg.Remove(0, 8); // 先頭10文字「/info * 」削除
                    break;
                case "/gift":
                    // 
                    // 2****7 はニコニコの ID
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
                    // string user = msg.Substring(fmNum, toNum); 落ちるから全部 remove で処理する
                    // 
                    // msg = /gift vcast_ocha 2****7 \"Taki\" 300 \"\" \"お茶\" 1
                    // msg = /gift vcast_ocha 2****7 \"Ta /,\ki(SPSLCY)\" 0 \"\" \"貝がら（6種ランダム）\" 1
                    // msg = /gift vcast_free_shell NULL \"名無し\" 0 \"\" \"貝がら（6種ランダム）\"
                    // 
                    int fmUser = msg.IndexOf("\"");
                    fmUser = fmUser + 1;

                    string user = msg.Remove(0, fmUser);
                    // 結果
                    // user = Taki\" 300 \"\" \"お茶\" 1
                    // user = Ta /,\ki(SPSLCY)\" 0 \"\" \"貝がら（6種ランダム）\" 1
                    // user = 名無し\" 0 \"\" \"貝がら（6種ランダム）\"

                    int toUser = user.LastIndexOf("\"");
                    toUser = user.LastIndexOf("\"", toUser - 1);
                    toUser = user.LastIndexOf("\"", toUser - 1);
                    toUser = user.LastIndexOf("\"", toUser - 1);
                    toUser = user.LastIndexOf("\"", toUser - 1);
                    toUser = toUser - 1;

                    user = user.Remove(toUser);
                    // 結果
                    // user = Taki
                    // user = Ta /,\ki(SPSLCY)
                    // user = 名無し

                    user = user.Replace("\n", "").Replace("\r", "");
                    user = user.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
                    user = user.Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    // 結果
                    // user = Taki
                    // user = Ta ／，＼ki(SPSLCY)
                    // user = 名無し

                    // pt
                    // msg = /gift vcast_ocha 2****7 \"Taki\" 300 \"\" \"お茶\" 1
                    // msg = /gift vcast_ocha 2****7 "Ta /,\ki(SPSLCY)\" 0 \"\" \"貝がら（6種ランダム）\" 1
                    // msg = /gift vcast_free_shell NULL \"名無し\" 0 \"\" \"貝がら（6種ランダム）\"
                    // 
                    // toUserNum + 2 からだけど、後ろから5回目の「\"」 + 2 にする
                    // 後ろから2回目の「 "」 - 1 までだけど、後ろから4回目の「\"」 - 2 にする
                    // 「\"」で統一
                    //
                    int fmPt = msg.LastIndexOf("\"");
                    fmPt = msg.LastIndexOf("\"", fmPt - 1);
                    fmPt = msg.LastIndexOf("\"", fmPt - 1);
                    fmPt = msg.LastIndexOf("\"", fmPt - 1);
                    fmPt = msg.LastIndexOf("\"", fmPt - 1);
                    fmPt = fmPt + 2;
                    string pt = msg.Remove(0, fmPt);
                    // 結果
                    // pt = 300 \"\" \"お茶\" 1
                    // pt = 0 \"\" \"貝がら（6種ランダム）\" 1
                    // pt = 0 \"\" \"貝がら（6種ランダム）\"

                    int toPt = pt.LastIndexOf("\"");
                    toPt = pt.LastIndexOf("\"", toPt - 1);
                    toPt = pt.LastIndexOf("\"", toPt - 1);
                    toPt = pt.LastIndexOf("\"", toPt - 1);
                    toPt = toPt - 2;

                    pt = pt.Remove(toPt);
                    // 結果
                    // pt = 300
                    // pt = 0
                    // pt = 0

                    // giftName
                    // msg = /gift vcast_ocha 2****7 \"Taki\" 300 \"\" \"お茶\" 1
                    // msg = /gift vcast_ocha 2****7 "Ta /,\ki(SPSLCY)\" 0 \"\" \"貝がら（6種ランダム）\" 1
                    // msg = /gift vcast_free_shell NULL \"名無し\" 0 \"\" \"貝がら（6種ランダム）\"
                    // 
                    // 後ろから2回目の「\"」 + 1 から
                    // 後ろから1回目の「\"」 - 1 まで
                    // 
                    int fmGiftName = msg.LastIndexOf("\"");
                    fmGiftName = msg.LastIndexOf("\"", fmGiftName - 1);
                    fmGiftName = fmGiftName + 1;

                    string giftName = msg.Remove(0, fmGiftName);
                    // 結果
                    // giftName = お茶\" 1
                    // giftName = 貝がら（6種ランダム）\" 1
                    // giftName = 貝がら（6種ランダム）\"

                    int toGiftName = giftName.LastIndexOf("\"");
                    toGiftName = toGiftName - 1;

                    giftName = giftName.Remove(toGiftName);
                    // 結果
                    // giftName = お茶
                    // giftName = 貝がら（6種ランダム）
                    // giftName = 貝がら（6種ランダム）

                    // rank 桁数不明
                    // 「名無し」の時は空
                    // msg = /gift vcast_ocha 2****7 \"Taki\" 300 \"\" \"お茶\" 1
                    // msg = /gift vcast_ocha 2****7 "Ta /,\ki(SPSLCY)\" 0 \"\" \"貝がら（6種ランダム）\" 1
                    // msg = /gift vcast_free_shell NULL \"名無し\" 0 \"\" \"貝がら（6種ランダム）\"
                    // 
                    // ID が NULL ではなかったら rank 処理
                    // 先頭から、後ろから1回目の「\"」+ 2 まで削除
                    // 
                    if (str[2] == "NULL")
                    {
                        msg = user + "さんがギフト「" + giftName + "（" + pt + "pt）」を贈りました";
                        // 結果
                        // 名無しさんがギフト「貝がら（6種ランダム）（0pt）」を贈りました
                    }
                    else
                    {
                        int fmRank = msg.LastIndexOf("\"");
                        fmRank = fmRank + 2;
                        string rank = msg.Remove(0, fmRank);

                        msg = "【ギフト貢献" + rank + "位】" + user + "さんがギフト「" + giftName + "（" + pt + "pt）」を贈りました";
                        // 結果
                        // 【ギフト貢献1位】Takiさんがギフト「お茶（300pt）」を贈りました
                        // 【ギフト貢献1位】Ta ／，＼ki(SPSLCY)さんがギフト「貝がら（6種ランダム）（0pt）」を贈りました
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
