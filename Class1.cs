// http://c-loft.com/blog/?p=719     この記事を参考に作成 webの公開情報
// https://github.com/chinng-inta    運営コメントの条件分岐等   MIT License
// https://github.com/oocytanb       CommentBaton から縦書きコメビュにメッセージを送る MIT License
//
// SPDX-License-Identifier: MIT
// 20200718 v1.0 Taki co1956457
//
using System;
using Plugin;
using System.Windows.Forms;
using System.IO;
using System.Text;

namespace NtoV

{
    public class Class1 : IPlugin
    {
        private IPluginHost _host = null;

        #region IPlugin メンバ

        bool ONOFF = true;
        int fileExist;
        string targetPath;

        /// <summary>
        /// IsAutoRunがtrueの場合、アプリケーション起動時に自動実行される
        /// </summary>
        public void AutoRun()
        {
            // ファイルの存在確認
            fileExist = fileExistError();

            if (fileExist == 0) // 問題なし
            {
                // コメント受信時のイベントハンドラ追加
                _host.ReceivedComment += new ReceivedCommentEventHandler(_host_ReceivedComment);

                // main.lua 初期化
                File.WriteAllText(targetPath, "");
            }
            else // 問題あり
            {
                showFileExistError(fileExist);
            }

            // 放送接続イベントハンドラ追加
            _host.BroadcastConnected += new BroadcastConnectedEventHandler(_host_BroadcastConnected);

            // 放送切断イベントハンドラ追加
            _host.BroadcastDisConnected += new BroadcastDisConnectedEventHandler(_host_BroadcastDisConnected);
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
        /// プラグインの名前
        /// </summary>
        public string Name
        {
            // get { throw new NotImplementedException(); }
            get { return "NtoV [停止/開始]"; }
        }

        /// <summary>
        /// プラグインを実行する
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
                    _host.ReceivedComment -= _host_ReceivedComment;
                    MessageBox.Show("停止しました。\n\nStopped", Name);
                    // main.lua 初期化
                    File.WriteAllText(targetPath, "");
                }
                else
                {
                    // プラグイン開始
                    ONOFF = true;
                    _host.ReceivedComment += _host_ReceivedComment;
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
        /// プラグインのバージョン
        /// </summary>
        public string Version
        {
            // get { throw new NotImplementedException(); }
            get { return "1.0"; }
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
                editComment(comment);
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
                _host.ReceivedComment -= _host_ReceivedComment;
                MessageBox.Show("プラグインを停止しました。\nThis plugin was stopped\n\n設定ファイルがありません。\nThere is no setting file.\n\n1. C:\\Users\\%ユーザー名%\\AppData\\Roaming\\posite-c\\NiconamaCommentViewer\\NtoV.txt を作成してください。\n   Please create the text file.\n\n2. NtoV.txt に CommentBaton VCI の場所 C:\\Users\\%ユーザー名%\\AppData\\LocalLow\\infiniteloopCo,Ltd\\VirtualCast\\EmbeddedScriptWorkspace\\CommentBaton を書いてください。\n   Please write the CommentBaton VCI directory in the text file.\n\n3. NCVを立ち上げなおしてください。\n   Please reboot NCV.", "NtoV エラー error");
            }
            else if (errorNumber == 2)
            {
                // プラグイン停止
                ONOFF = false;
                _host.ReceivedComment -= _host_ReceivedComment;
                MessageBox.Show("プラグインを停止しました。\nThis plugin was stopped\n\n指定ディレクトリが CommentBaton ではありません。\nThe directory is not CommentBaton.\n\n1. NtoV.txt の内容（ CommentBaton VCI の場所 ）を確認してください。\n   Please check the CommentBaton directory in the NtoV.txt.\n\n2. NCVを立ち上げなおしてください。\n   Please reboot NCV.", "NtoV エラー error");
            }
            else if (errorNumber == 3)
            {
                // プラグイン停止
                ONOFF = false;
                _host.ReceivedComment -= _host_ReceivedComment;
                MessageBox.Show("プラグインを停止しました。\nThis plugin was stopped\n\n指定ディレクトリがありません。\nThe directory does not Exist.\n\n1. NtoV.txt の内容（ CommentBaton VCI の場所 ）と実在を確認してください。\n   Please check the CommentBaton directory in the NtoV.txt and existence.\n\n2. NCVを立ち上げなおしてください。\n   Please reboot NCV.", "NtoV エラー error");
            }
        }

        /// <summary>
        /// 運営コメントを編集
        /// 参考 https://github.com/chinng-inta
        /// </summary>
        void editComment(string message)
        {
            string msg = message.Replace("\"", "\\\"");
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

                    adMessage = adMessage.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    adMessage = adMessage.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\\\", "＼");
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
                    msg = msg.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    msg = msg.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
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

                    user = user.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    user = user.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
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
                    msg = msg.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    msg = msg.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
                    break;
                case "/cruise":
                    // 
                    // /cruise \"まもなく生放送クルーズが到着します\"
                    // /cruise \"生放送クルーズが去っていきます\"
                    // 
                    msg = msg.Remove(0, 10); // 先頭10文字「/cruise \"」削除
                    msg = msg.TrimEnd('\"');
                    msg = msg.TrimEnd('\\');
                    msg = msg.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    msg = msg.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
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
                    msg = msg.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    msg = msg.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
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
                    msg = msg.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    msg = msg.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
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
                                    int dotZero = str[i].IndexOf(".0");
                                    if (dotZero != -1)
                                    {
                                        str[i] = str[i].Remove(dotZero); // 5.0 -> 5
                                    }
                                }
                                else if (striLen == 3)
                                {
                                    str[i] = str[i].Insert(2, "."); // 917 -> 91.7

                                    int dotZero = str[i].IndexOf(".0");
                                    if (dotZero != -1)
                                    {
                                        str[i] = str[i].Remove(dotZero); // 75.0 -> 75
                                    }
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
                    msg = msg.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    msg = msg.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
                    break;
                case "<a":
                    //
                    // <a href="https://example.jp/example/example.html?from=live_watch_anime202099_player" target="_blank"><u>今期アニメ　niconicoでの配信一覧はこちら！｜Nアニメ</u></a>
                    // 今期アニメ　niconicoでの配信一覧はこちら！｜Nアニメ
                    //
                    // タグ <a> と <u> を外す
                    msg = "＜リンク＞" + removeA(msg);
                    msg = msg.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    msg = msg.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
                    break;
                case "<u><font":
                    //
                    // <u><font color="#00CCFF"><a href="https://www.nicovideo.jp/watch/sm36179129" class="video" target="_blank">sm36179129</a></font></u> てすと
                    // sm36179129 BGM「よいしょ（Yoisho）」
                    //
                    // タグ <u> と <font> と <a> を外す removeA とは別
                    msg = "＜リンク＞" + removeUFA(msg);
                    msg = msg.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    msg = msg.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
                    break;
                // 
                // デフォルト処理でいい
                // case "/disconnect":
                //    msg = "disconnect";
                //    msg = msg.Replace("\\\"", "");
                //    break;
                // 
                default:
                    msg = msg.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
                    msg = msg.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
                    break;
            }
            // 編集した運営コメントを CommentBaton に渡す
            // msg = message; // Debug
            toCommentBaton(msg);
        }

        /// <summary>
        /// 編集した運営コメントを CommentBaton に渡す
        /// https://github.com/oocytanb
        /// </summary>
        void toCommentBaton(string msg)
        {
            string s1;
            string s2;

            // cytanb.EmitCommentMessage の実行は IsMine の中に書くこと。そうしないとゲストの人数分実行されてしまう。
            s1 = "-- SPDX-License-Identifier: MIT\n-- Copyright (c) 2019 oO (https://github.com/oocytanb)\n---@type cytanb @See `cytanb_annotations.lua`\nlocal cytanb=(function()local b=\'__CYTANB_INSTANCE_ID\'local c;local d;local e;local f;local g=false;local h;local i;local j;local a;local k=function(l,m)for n=1,4 do local o=l[n]-m[n]if o~=0 then return o end end;return 0 end;local p;p={__eq=function(l,m)return l[1]==m[1]and l[2]==m[2]and l[3]==m[3]and l[4]==m[4]end,__lt=function(l,m)return k(l,m)<0 end,__le=function(l,m)return k(l,m)<=0 end,__tostring=function(q)local r=q[2]or 0;local s=q[3]or 0;return string.format(\'%08x-%04x-%04x-%04x-%04x%08x\',bit32.band(q[1]or 0,0xFFFFFFFF),bit32.band(bit32.rshift(r,16),0xFFFF),bit32.band(r,0xFFFF),bit32.band(bit32.rshift(s,16),0xFFFF),bit32.band(s,0xFFFF),bit32.band(q[4]or 0,0xFFFFFFFF))end,__concat=function(l,m)local t=getmetatable(l)local u=t==p or type(t)==\'table\'and t.__concat==p.__concat;local v=getmetatable(m)local w=v==p or type(v)==\'table\'and v.__concat==p.__concat;if not u and not w then error(\'UUID: attempt to concatenate illegal values\',2)end;return(u and p.__tostring(l)or l)..(w and p.__tostring(m)or m)end}local x=\'__CYTANB_CONST_VARIABLES\'local y=function(table,z)local A=getmetatable(table)if A then local B=rawget(A,x)if B then local C=rawget(B,z)if type(C)==\'function\'then return C(table,z)else return C end end end;return nil end;local D=function(table,z,E)local A=getmetatable(table)if A then local B=rawget(A,x)if B then if rawget(B,z)~=nil then error(\'Cannot assign to read only field \"\'..z..\'\"\',2)end end end;rawset(table,z,E)end;local F=function(G,H)local I=G[a.TypeParameterName]if a.NillableHasValue(I)and a.NillableValue(I)~=H then return false,false end;return a.NillableIfHasValueOrElse(c[H],function(J)local K=J.compositionFieldNames;local L=J.compositionFieldLength;local M=false;for N,E in pairs(G)do if K[N]then L=L-1;if L<=0 and M then break end elseif N~=a.TypeParameterName then M=true;if L<=0 then break end end end;return L<=0,M end,function()return false,false end)end;local O=function(P)return string.gsub(string.gsub(P,a.EscapeSequenceTag,{[a.EscapeSequenceTag]=a.EscapeSequenceTag..a.EscapeSequenceTag}),\'/\',{[\'/\']=a.SolidusTag})end;local Q=function(P,R)local S=string.len(P)local T=string.len(a.EscapeSequenceTag)if T>S then return P end;local U=\'\'local n=1;while n<S do local V,W=string.find(P,a.EscapeSequenceTag,n,true)if not V then if n==1 then U=P else U=U..string.sub(P,n)end;break end;if V>n then U=U..string.sub(P,n,V-1)end;local X=false;for Y,Z in ipairs(d)do local _,a0=string.find(P,Z.pattern,V)if _ then U=U..(R and R(Z.tag)or Z.replacement)n=a0+1;X=true;break end end;if not X then U=U..a.EscapeSequenceTag;n=W+1 end end;return U end;local a1;a1=function(a2,a3)if type(a2)~=\'table\'then return a2 end;if not a3 then a3={}end;if a3[a2]then error(\'circular reference\')end;a3[a2]=true;local a4={}for N,E in pairs(a2)do local a5=type(N)local a6;if a5==\'string\'then a6=O(N)elseif a5==\'number\'then a6=tostring(N)..a.ArrayNumberTag else a6=N end;local a7=type(E)if a7==\'string\'then a4[a6]=O(E)elseif a7==\'number\'and E<0 then a4[tostring(a6)..a.NegativeNumberTag]=tostring(E)else a4[a6]=a1(E,a3)end end;a3[a2]=nil;return a4 end;local a8;a8=function(a4,a9)if type(a4)~=\'table\'then return a4 end;local a2={}for N,E in pairs(a4)do local a6;local aa=false;if type(N)==\'string\'then local ab=false;a6=Q(N,function(ac)if ac==a.NegativeNumberTag then aa=true elseif ac==a.ArrayNumberTag then ab=true end;return nil end)if ab then a6=tonumber(a6)or a6 end else a6=N;aa=false end;if aa and type(E)==\'string\'then a2[a6]=tonumber(E)elseif type(E)==\'string\'then a2[a6]=Q(E,function(ac)return e[ac]end)else a2[a6]=a8(E,a9)end end;if not a9 then a.NillableIfHasValue(a2[a.TypeParameterName],function(ad)a.NillableIfHasValue(c[ad],function(J)local ae,M=J.fromTableFunc(a2)if not M then a.NillableIfHasValue(ae,function(q)a2=q end)end end)end)end;return a2 end;a={InstanceID=function()if i==\'\'then i=vci.state.Get(b)or\'\'end;return i end,NillableHasValue=function(af)return af~=nil end,NillableValue=function(af)if af==nil then error(\'nillable: value is nil\',2)end;return af end,NillableValueOrDefault=function(af,ag)if af==nil then if ag==nil then error(\'nillable: defaultValue is nil\',2)end;return ag else return af end end,NillableIfHasValue=function(af,ah)if af==nil then return nil else return ah(af)end end,NillableIfHasValueOrElse=function(af,ah,ai)if af==nil then return ai()else return ah(af)end end,StringReplace=function(P,aj,ak)local al;local S=string.len(P)if aj==\'\'then al=ak;for n=1,S do al=al..string.sub(P,n,n)..ak end else al=\'\'local n=1;while true do local am,V=string.find(P,aj,n,true)if am then al=al..string.sub(P,n,am-1)..ak;n=V+1;if n>S then break end else al=n==1 and P or al..string.sub(P,n)break end end end;return al end,SetConst=function(aj,an,q)if type(aj)~=\'table\'then error(\'Cannot set const to non-table target\',2)end;local ao=getmetatable(aj)local A=ao or{}local ap=rawget(A,x)if rawget(aj,an)~=nil then error(\'Non-const field \"\'..an..\'\" already exists\',2)end;if not ap then ap={}rawset(A,x,ap)A.__index=y;A.__newindex=D end;rawset(ap,an,q)if not ao then setmetatable(aj,A)end;return aj end,SetConstEach=function(aj,aq)for N,E in pairs(aq)do a.SetConst(aj,N,E)end;return aj end,Extend=function(aj,ar,as,at,a3)if aj==ar or type(aj)~=\'table\'or type(ar)~=\'table\'then return aj end;if as then if not a3 then a3={}end;if a3[ar]then error(\'circular reference\')end;a3[ar]=true end;for N,E in pairs(ar)do if as and type(E)==\'table\'then local au=aj[N]aj[N]=a.Extend(type(au)==\'table\'and au or{},E,as,at,a3)else aj[N]=E end end;if not at then local av=getmetatable(ar)if type(av)==\'table\'then if as then local aw=getmetatable(aj)setmetatable(aj,a.Extend(type(aw)==\'table\'and aw or{},av,true))else setmetatable(aj,av)end end end;if as then a3[ar]=nil end;return aj end,Vars=function(E,ax,ay,a3)local az;if ax then az=ax~=\'__NOLF\'else ax=\'  \'az=true end;if not ay then ay=\'\'end;if not a3 then a3={}end;local aA=type(E)if aA==\'table\'then a3[E]=a3[E]and a3[E]+1 or 1;local aB=az and ay..ax or\'\'local P=\'(\'..tostring(E)..\') {\'local aC=true;for z,aD in pairs(E)do if aC then aC=false else P=P..(az and\',\'or\', \')end;if az then P=P..\'\\n\'..aB end;if type(aD)==\'table\'and a3[aD]and a3[aD]>0 then P=P..z..\' = (\'..tostring(aD)..\')\'else P=P..z..\' = \'..a.Vars(aD,ax,aB,a3)end end;if not aC and az then P=P..\'\\n\'..ay end;P=P..\'}\'a3[E]=a3[E]-1;if a3[E]<=0 then a3[E]=nil end;return P elseif aA==\'function\'or aA==\'thread\'or aA==\'userdata\'then return\'(\'..aA..\')\'elseif aA==\'string\'then return\'(\'..aA..\') \'..string.format(\'%q\',E)else return\'(\'..aA..\') \'..tostring(E)end end,GetLogLevel=function()return f end,SetLogLevel=function(aE)f=aE end,IsOutputLogLevelEnabled=function()return g end,SetOutputLogLevelEnabled=function(aF)g=not not aF end,Log=function(aE,...)if aE<=f then local aG=g and(h[aE]or\'LOG LEVEL \'..tostring(aE))..\' | \'or\'\'local aH=table.pack(...)if aH.n==1 then local E=aH[1]if E~=nil then local P=type(E)==\'table\'and a.Vars(E)or tostring(E)print(g and aG..P or P)else print(aG)end else local P=aG;for n=1,aH.n do local E=aH[n]if E~=nil then P=P..(type(E)==\'table\'and a.Vars(E)or tostring(E))end end;print(P)end end end,LogFatal=function(...)a.Log(a.LogLevelFatal,...)end,LogError=function(...)a.Log(a.LogLevelError,...)end,LogWarn=function(...)a.Log(a.LogLevelWarn,...)end,LogInfo=function(...)a.Log(a.LogLevelInfo,...)end,LogDebug=function(...)a.Log(a.LogLevelDebug,...)end,LogTrace=function(...)a.Log(a.LogLevelTrace,...)end,FatalLog=function(...)a.LogFatal(...)end,ErrorLog=function(...)a.LogError(...)end,WarnLog=function(...)a.LogWarn(...)end,InfoLog=function(...)a.LogInfo(...)end,DebugLog=function(...)a.LogDebug(...)end,TraceLog=function(...)a.LogTrace(...)end,ListToMap=function(aI,aJ)local aK={}if aJ==nil then for N,E in pairs(aI)do aK[E]=E end elseif type(aJ)==\'function\'then for N,E in pairs(aI)do local aL,aM=aJ(E)aK[aL]=aM end else for N,E in pairs(aI)do aK[E]=aJ end end;return aK end,Round=function(aN,aO)if aO then local aP=10^aO;return math.floor(aN*aP+0.5)/aP else return math.floor(aN+0.5)end end,Clamp=function(q,aQ,aR)return math.max(aQ,math.min(q,aR))end,Lerp=function(aS,aT,aA)if aA<=0.0 then return aS elseif aA>=1.0 then return aT else return aS+(aT-aS)*aA end end,LerpUnclamped=function(aS,aT,aA)if aA==0.0 then return aS elseif aA==1.0 then return aT else return aS+(aT-aS)*aA end end,PingPong=function(aA,aU)if aU==0 then return 0,1 end;local aV=math.floor(aA/aU)local aW=aA-aV*aU;if aV<0 then if(aV+1)%2==0 then return aU-aW,-1 else return aW,1 end else if aV%2==0 then return aW,1 else return aU-aW,-1 end end end,VectorApproximatelyEquals=function(aX,aY)return(aX-aY).sqrMagnitude<1E-10 end,QuaternionApproximatelyEquals=function(aX,aY)local aZ=Quaternion.Dot(aX,aY)return aZ<1.0+1E-06 and aZ>1.0-1E-06 end,\nQuaternionToAngleAxis=function(a_)local aV=a_.normalized;local b0=math.acos(aV.w)local b1=math.sin(b0)local b2=math.deg(b0*2.0)local b3;if math.abs(b1)<=Quaternion.kEpsilon then b3=Vector3.right else local am=1.0/b1;b3=Vector3.__new(aV.x*am,aV.y*am,aV.z*am)end;return b2,b3 end,QuaternionTwist=function(a_,b4)if b4.sqrMagnitude<Vector3.kEpsilonNormalSqrt then return Quaternion.identity end;local b5=Vector3.__new(a_.x,a_.y,a_.z)if b5.sqrMagnitude>=Vector3.kEpsilonNormalSqrt then local b6=Vector3.Project(b5,b4)if b6.sqrMagnitude>=Vector3.kEpsilonNormalSqrt then local b7=Quaternion.__new(b6.x,b6.y,b6.z,a_.w)b7.Normalize()return b7 else return Quaternion.AngleAxis(0,b4)end else local b8=a.QuaternionToAngleAxis(a_)return Quaternion.AngleAxis(b8,b4)end end,ApplyQuaternionToVector3=function(a_,b9)local ba=a_.w*b9.x+a_.y*b9.z-a_.z*b9.y;local bb=a_.w*b9.y-a_.x*b9.z+a_.z*b9.x;local bc=a_.w*b9.z+a_.x*b9.y-a_.y*b9.x;local bd=-a_.x*b9.x-a_.y*b9.y-a_.z*b9.z;return Vector3.__new(bd*-a_.x+ba*a_.w+bb*-a_.z-bc*-a_.y,bd*-a_.y-ba*-a_.z+bb*a_.w+bc*-a_.x,bd*-a_.z+ba*-a_.y-bb*-a_.x+bc*a_.w)end,RotateAround=function(be,bf,bg,bh)return bg+bh*(be-bg),bh*bf end,Random32=function()return bit32.band(math.random(-2147483648,2147483646),0xFFFFFFFF)end,RandomUUID=function()return a.UUIDFromNumbers(a.Random32(),bit32.bor(0x4000,bit32.band(a.Random32(),0xFFFF0FFF)),bit32.bor(0x80000000,bit32.band(a.Random32(),0x3FFFFFFF)),a.Random32())end,UUIDString=function(bi)return p.__tostring(bi)end,UUIDFromNumbers=function(...)local bj=...local aA=type(bj)local bk,bl,bm,bn;if aA==\'table\'then bk=bj[1]bl=bj[2]bm=bj[3]bn=bj[4]else bk,bl,bm,bn=...end;local bi={bit32.band(bk or 0,0xFFFFFFFF),bit32.band(bl or 0,0xFFFFFFFF),bit32.band(bm or 0,0xFFFFFFFF),bit32.band(bn or 0,0xFFFFFFFF)}setmetatable(bi,p)return bi end,UUIDFromString=function(P)local S=string.len(P)if S~=32 and S~=36 then return nil end;local bo=\'[0-9a-f-A-F]+\'local bp=\'^(\'..bo..\')$\'local bq=\'^-(\'..bo..\')$\'local br,bs,bt,bu;if S==32 then local bi=a.UUIDFromNumbers(0,0,0,0)local bv=1;for n,bw in ipairs({8,16,24,32})do br,bs,bt=string.find(string.sub(P,bv,bw),bp)if not br then return nil end;bi[n]=tonumber(bt,16)bv=bw+1 end;return bi else br,bs,bt=string.find(string.sub(P,1,8),bp)if not br then return nil end;local bk=tonumber(bt,16)br,bs,bt=string.find(string.sub(P,9,13),bq)if not br then return nil end;br,bs,bu=string.find(string.sub(P,14,18),bq)if not br then return nil end;local bl=tonumber(bt..bu,16)br,bs,bt=string.find(string.sub(P,19,23),bq)if not br then return nil end;br,bs,bu=string.find(string.sub(P,24,28),bq)if not br then return nil end;local bm=tonumber(bt..bu,16)br,bs,bt=string.find(string.sub(P,29,36),bp)if not br then return nil end;local bn=tonumber(bt,16)return a.UUIDFromNumbers(bk,bl,bm,bn)end end,ParseUUID=function(P)return a.UUIDFromString(P)end,CreateCircularQueue=function(bx)if type(bx)~=\'number\'or bx<1 then error(\'CreateCircularQueue: Invalid argument: capacity = \'..tostring(bx),2)end;local self;local by=math.floor(bx)local U={}local bz=0;local bA=0;local bB=0;self={Size=function()return bB end,Clear=function()bz=0;bA=0;bB=0 end,IsEmpty=function()return bB==0 end,Offer=function(bC)U[bz+1]=bC;bz=(bz+1)%by;if bB<by then bB=bB+1 else bA=(bA+1)%by end;return true end,OfferFirst=function(bC)bA=(by+bA-1)%by;U[bA+1]=bC;if bB<by then bB=bB+1 else bz=(by+bz-1)%by end;return true end,Poll=function()if bB==0 then return nil else local bC=U[bA+1]bA=(bA+1)%by;bB=bB-1;return bC end end,PollLast=function()if bB==0 then return nil else bz=(by+bz-1)%by;local bC=U[bz+1]bB=bB-1;return bC end end,Peek=function()if bB==0 then return nil else return U[bA+1]end end,PeekLast=function()if bB==0 then return nil else return U[(by+bz-1)%by+1]end end,Get=function(bD)if bD<1 or bD>bB then a.LogError(\'CreateCircularQueue.Get: index is outside the range: \'..bD)return nil end;return U[(bA+bD-1)%by+1]end,IsFull=function()return bB>=by end,MaxSize=function()return by end}return self end,DetectClicks=function(bE,bF,bG)local bH=bE or 0;local bI=bG or TimeSpan.FromMilliseconds(500)local bJ=vci.me.Time;local bK=bF and bJ>bF+bI and 1 or bH+1;return bK,bJ end,ColorRGBToHSV=function(bL)local aW=math.max(0.0,math.min(bL.r,1.0))local bM=math.max(0.0,math.min(bL.g,1.0))local aT=math.max(0.0,math.min(bL.b,1.0))local aR=math.max(aW,bM,aT)local aQ=math.min(aW,bM,aT)local bN=aR-aQ;local C;if bN==0.0 then C=0.0 elseif aR==aW then C=(bM-aT)/bN/6.0 elseif aR==bM then C=(2.0+(aT-aW)/bN)/6.0 else C=(4.0+(aW-bM)/bN)/6.0 end;if C<0.0 then C=C+1.0 end;local bO=aR==0.0 and bN or bN/aR;local E=aR;return C,bO,E end,ColorFromARGB32=function(bP)local bQ=type(bP)==\'number\'and bP or 0xFF000000;return Color.__new(bit32.band(bit32.rshift(bQ,16),0xFF)/0xFF,bit32.band(bit32.rshift(bQ,8),0xFF)/0xFF,bit32.band(bQ,0xFF)/0xFF,bit32.band(bit32.rshift(bQ,24),0xFF)/0xFF)end,ColorToARGB32=function(bL)return bit32.bor(bit32.lshift(bit32.band(a.Round(0xFF*bL.a),0xFF),24),bit32.lshift(bit32.band(a.Round(0xFF*bL.r),0xFF),16),bit32.lshift(bit32.band(a.Round(0xFF*bL.g),0xFF),8),bit32.band(a.Round(0xFF*bL.b),0xFF))end,ColorFromIndex=function(bR,bS,bT,bU,bV)local bW=math.max(math.floor(bS or a.ColorHueSamples),1)local bX=bV and bW or bW-1;local bY=math.max(math.floor(bT or a.ColorSaturationSamples),1)local bZ=math.max(math.floor(bU or a.ColorBrightnessSamples),1)local bD=a.Clamp(math.floor(bR or 0),0,bW*bY*bZ-1)local b_=bD%bW;local c0=math.floor(bD/bW)local am=c0%bY;local c1=math.floor(c0/bY)if bV or b_~=bX then local C=b_/bX;local bO=(bY-am)/bY;local E=(bZ-c1)/bZ;return Color.HSVToRGB(C,bO,E)else local E=(bZ-c1)/bZ*am/(bY-1)return Color.HSVToRGB(0.0,0.0,E)end end,ColorToIndex=function(bL,bS,bT,bU,bV)local bW=math.max(math.floor(bS or a.ColorHueSamples),1)local bX=bV and bW or bW-1;local bY=math.max(math.floor(bT or a.ColorSaturationSamples),1)local bZ=math.max(math.floor(bU or a.ColorBrightnessSamples),1)local C,bO,E=a.ColorRGBToHSV(bL)local am=a.Round(bY*(1.0-bO))if bV or am<bY then local c2=a.Round(bX*C)if c2>=bX then c2=0 end;if am>=bY then am=bY-1 end;local c1=math.min(bZ-1,a.Round(bZ*(1.0-E)))return c2+bW*(am+bY*c1)else local c3=a.Round((bY-1)*E)if c3==0 then local c4=a.Round(bZ*(1.0-E))if c4>=bZ then return bW-1 else return bW*(1+a.Round(E*(bY-1)/(bZ-c4)*bZ)+bY*c4)-1 end else return bW*(1+c3+bY*a.Round(bZ*(1.0-E*(bY-1)/c3)))-1 end end end,ColorToTable=function(bL)return{[a.TypeParameterName]=a.ColorTypeName,r=bL.r,g=bL.g,b=bL.b,a=bL.a}end,ColorFromTable=function(G)local aT,M=F(G,a.ColorTypeName)return aT and Color.__new(G.r,G.g,G.b,G.a)or nil,M end,Vector2ToTable=function(q)return{[a.TypeParameterName]=a.Vector2TypeName,x=q.x,y=q.y}end,Vector2FromTable=function(G)local aT,M=F(G,a.Vector2TypeName)return aT and Vector2.__new(G.x,G.y)or nil,M end,Vector3ToTable=function(q)return{[a.TypeParameterName]=a.Vector3TypeName,x=q.x,y=q.y,z=q.z}end,Vector3FromTable=function(G)local aT,M=F(G,a.Vector3TypeName)return aT and Vector3.__new(G.x,G.y,G.z)or nil,M end,Vector4ToTable=function(q)return{[a.TypeParameterName]=a.Vector4TypeName,x=q.x,y=q.y,z=q.z,w=q.w}end,Vector4FromTable=function(G)local aT,M=F(G,a.Vector4TypeName)return aT and Vector4.__new(G.x,G.y,G.z,G.w)or nil,M end,QuaternionToTable=function(q)return{[a.TypeParameterName]=a.QuaternionTypeName,x=q.x,y=q.y,z=q.z,w=q.w}end,QuaternionFromTable=function(G)local aT,M=F(G,a.QuaternionTypeName)return aT and Quaternion.__new(G.x,G.y,G.z,G.w)or nil,M end,TableToSerializable=function(a2)return a1(a2)end,TableFromSerializable=function(a4,a9)return a8(a4,a9)end,TableToSerialiable=function(a2)return a1(a2)end,TableFromSerialiable=function(a4,a9)return a8(a4,a9)end,EmitMessage=function(an,c5)local a4=a.NillableIfHasValueOrElse(c5,function(a2)if type(a2)~=\'table\'then error(\'EmitMessage: Invalid argument: table expected\',3)end;return a.TableToSerializable(a2)end,function()return{}end)a4[a.InstanceIDParameterName]=a.InstanceID()vci.message.Emit(an,json.serialize(a4))end,OnMessage=function(an,ah)local c6=function(c7,c8,c9)if type(c9)==\'string\'and c9~=\'\'and string.sub(c9,1,1)==\'{\'then local ca,a4=pcall(json.parse,c9)if ca and type(a4)==\'table\'and a4[a.InstanceIDParameterName]then local cb=a.TableFromSerializable(a4)local cc;local cd=cb[a.MessageSenderOverride]if cd then cc=a.Extend(a.Extend({},c7,true),cd,true)cc[a.MessageOriginalSender]=c7 else cc=c7 end;ah(cc,c8,cb)return end end;ah(c7,c8,{[a.MessageValueParameterName]=c9})end;vci.message.On(an,c6)return{Off=function()if c6 then c6=nil end end}end,OnInstanceMessage=function(an,ah)local c6=function(c7,c8,c5)local ce=a.InstanceID()if ce~=\'\'and ce==c5[a.InstanceIDParameterName]then ah(c7,c8,c5)end end;return a.OnMessage(an,c6)end,\nEmitCommentMessage=function(c9,cd)local cf={type=\'comment\',name=\'\',commentSource=\'\'}local c5={[a.MessageValueParameterName]=tostring(c9),[a.MessageSenderOverride]=type(cd)==\'table\'and a.Extend(cf,cd,true)or cf}a.EmitMessage(\'comment\',c5)end,OnCommentMessage=function(ah)local c6=function(c7,c8,c5)local c9=tostring(c5[a.MessageValueParameterName]or\'\')ah(c7,c8,c9)end;return a.OnMessage(\'comment\',c6)end,EmitNotificationMessage=function(c9,cd)local cf={type=\'notification\',name=\'\',commentSource=\'\'}local c5={[a.MessageValueParameterName]=tostring(c9),[a.MessageSenderOverride]=type(cd)==\'table\'and a.Extend(cf,cd,true)or cf}a.EmitMessage(\'notification\',c5)end,OnNotificationMessage=function(ah)local c6=function(c7,c8,c5)local c9=tostring(c5[a.MessageValueParameterName]or\'\')ah(c7,c8,c9)end;return a.OnMessage(\'notification\',c6)end,GetEffekseerEmitterMap=function(an)local cg=vci.assets.GetEffekseerEmitters(an)if not cg then return nil end;local aK={}for n,ch in pairs(cg)do aK[ch.EffectName]=ch end;return aK end,ClientID=function()return j end,ParseTagString=function(P)local ci=string.find(P,\'#\',1,true)if not ci then return{},P end;local cj={}local ck=string.sub(P,1,ci-1)ci=ci+1;local S=string.len(P)local cl=\'^[A-Za-z0-9_%-.()!~*\\'%%]+\'while ci<=S do local cm,cn=string.find(P,cl,ci)if cm then local co=string.sub(P,cm,cn)local cp=co;ci=cn+1;if ci<=S and string.sub(P,ci,ci)==\'=\'then ci=ci+1;local cq,cr=string.find(P,cl,ci)if cq then cp=string.sub(P,cq,cr)ci=cr+1 else cp=\'\'end end;cj[co]=cp end;ci=string.find(P,\'#\',ci,true)if not ci then break end;ci=ci+1 end;return cj,ck end,CalculateSIPrefix=(function()local cs=9;local ct={\'y\',\'z\',\'a\',\'f\',\'p\',\'n\',\'u\',\'m\',\'\',\'k\',\'M\',\'G\',\'T\',\'P\',\'E\',\'Z\',\'Y\'}local cu=#ct;return function(aN)local cv=aN==0 and 0 or a.Clamp(math.floor(math.log(math.abs(aN),1000)),1-cs,cu-cs)return cv==0 and aN or aN/1000^cv,ct[cs+cv],cv*3 end end)(),CreateLocalSharedProperties=function(cw,cx)local cy=TimeSpan.FromSeconds(5)local cz=\'33657f0e-7c44-4ee7-acd9-92dd8b8d807a\'local cA=\'__CYTANB_LOCAL_SHARED_PROPERTIES_LISTENER_MAP\'if type(cw)~=\'string\'or string.len(cw)<=0 or type(cx)~=\'string\'or string.len(cx)<=0 then error(\'LocalSharedProperties: Invalid arguments\',2)end;local cB=_G[cz]if not cB then cB={}_G[cz]=cB end;cB[cx]=vci.me.UnscaledTime;local cC=_G[cw]if not cC then cC={[cA]={}}_G[cw]=cC end;local cD=cC[cA]local self;self={GetLspID=function()return cw end,GetLoadID=function()return cx end,GetProperty=function(z,ag)local q=cC[z]if q==nil then return ag else return q end end,SetProperty=function(z,q)if z==cA then error(\'LocalSharedProperties: Invalid argument: key = \',z,2)end;local bJ=vci.me.UnscaledTime;local cE=cC[z]cC[z]=q;for cF,ce in pairs(cD)do local aA=cB[ce]if aA and aA+cy>=bJ then cF(self,z,q,cE)else cF(self,a.LOCAL_SHARED_PROPERTY_EXPIRED_KEY,true,false)cD[cF]=nil;cB[ce]=nil end end end,Clear=function()for z,q in pairs(cC)do if z~=cA then self.SetProperty(z,nil)end end end,Each=function(ah)for z,q in pairs(cC)do if z~=cA and ah(q,z,self)==false then return false end end end,AddListener=function(cF)cD[cF]=cx end,RemoveListener=function(cF)cD[cF]=nil end,UpdateAlive=function()cB[cx]=vci.me.UnscaledTime end}return self end,EstimateFixedTimestep=function(cG)local cH=1.0;local cI=1000.0;local cJ=TimeSpan.FromSeconds(0.02)local cK=0xFFFF;local cL=a.CreateCircularQueue(64)local cM=TimeSpan.FromSeconds(5)local cN=TimeSpan.FromSeconds(30)local cO=false;local cP=vci.me.Time;local cQ=a.Random32()local cR=Vector3.__new(bit32.bor(0x400,bit32.band(cQ,0x1FFF)),bit32.bor(0x400,bit32.band(bit32.rshift(cQ,16),0x1FFF)),0.0)cG.SetPosition(cR)cG.SetRotation(Quaternion.identity)cG.SetVelocity(Vector3.zero)cG.SetAngularVelocity(Vector3.zero)cG.AddForce(Vector3.__new(0.0,0.0,cH*cI))local self={Timestep=function()return cJ end,Precision=function()return cK end,IsFinished=function()return cO end,Update=function()if cO then return cJ end;local cS=vci.me.Time-cP;local cT=cS.TotalSeconds;if cT<=Vector3.kEpsilon then return cJ end;local cU=cG.GetPosition().z-cR.z;local cV=cU/cT;local cW=cV/cI;if cW<=Vector3.kEpsilon then return cJ end;cL.Offer(cW)local cX=cL.Size()if cX>=2 and cS>=cM then local cY=0.0;for n=1,cX do cY=cY+cL.Get(n)end;local cZ=cY/cX;local c_=0.0;for n=1,cX do c_=c_+(cL.Get(n)-cZ)^2 end;local d0=c_/cX;if d0<cK then cK=d0;cJ=TimeSpan.FromSeconds(cZ)end;if cS>cN then cO=true;cG.SetPosition(cR)cG.SetRotation(Quaternion.identity)cG.SetVelocity(Vector3.zero)cG.SetAngularVelocity(Vector3.zero)end else cJ=TimeSpan.FromSeconds(cW)end;return cJ end}return self end,AlignSubItemOrigin=function(d1,d2,d3)local d4=d1.GetRotation()if not a.QuaternionApproximatelyEquals(d2.GetRotation(),d4)then d2.SetRotation(d4)end;local d5=d1.GetPosition()if not a.VectorApproximatelyEquals(d2.GetPosition(),d5)then d2.SetPosition(d5)end;if d3 then d2.SetVelocity(Vector3.zero)d2.SetAngularVelocity(Vector3.zero)end end,CreateSubItemGlue=function()local d6={}local self;self={Contains=function(d7,d8)return a.NillableIfHasValueOrElse(d6[d7],function(aq)return a.NillableHasValue(aq[d8])end,function()return false end)end,Add=function(d7,d9,d3)if not d7 or not d9 then local da=\'SubItemGlue.Add: Invalid arguments \'..(not d7 and\', parent = \'..tostring(d7)or\'\')..(not d9 and\', children = \'..tostring(d9)or\'\')error(da,2)end;local aq=a.NillableIfHasValueOrElse(d6[d7],function(db)return db end,function()local db={}d6[d7]=db;return db end)if type(d9)==\'table\'then for z,aD in pairs(d9)do aq[aD]={velocityReset=not not d3}end else aq[d9]={velocityReset=not not d3}end end,Remove=function(d7,d8)return a.NillableIfHasValueOrElse(d6[d7],function(aq)if a.NillableHasValue(aq[d8])then aq[d8]=nil;return true else return false end end,function()return false end)end,RemoveParent=function(d7)if a.NillableHasValue(d6[d7])then d6[d7]=nil;return true else return false end end,RemoveAll=function()d6={}return true end,Each=function(ah,dc)return a.NillableIfHasValueOrElse(dc,function(d7)return a.NillableIfHasValue(d6[d7],function(aq)for d8,dd in pairs(aq)do if ah(d8,d7,self)==false then return false end end end)end,function()for d7,aq in pairs(d6)do if self.Each(ah,d7)==false then return false end end end)end,Update=function(de)for d7,aq in pairs(d6)do local df=d7.GetPosition()local dg=d7.GetRotation()for d8,dd in pairs(aq)do if de or d8.IsMine then if not a.QuaternionApproximatelyEquals(d8.GetRotation(),dg)then d8.SetRotation(dg)end;if not a.VectorApproximatelyEquals(d8.GetPosition(),df)then d8.SetPosition(df)end;if dd.velocityReset then d8.SetVelocity(Vector3.zero)d8.SetAngularVelocity(Vector3.zero)end end end end end}return self end,CreateUpdateRoutine=function(dh,di)return coroutine.wrap(function()local dj=TimeSpan.FromSeconds(30)local dk=vci.me.UnscaledTime;local dl=dk;local bF=vci.me.Time;local dm=true;while true do local ce=a.InstanceID()if ce~=\'\'then break end;local dn=vci.me.UnscaledTime;if dn-dj>dk then a.LogError(\'TIMEOUT: Could not receive Instance ID.\')return-1 end;dl=dn;bF=vci.me.Time;dm=false;coroutine.yield(100)end;if dm then dl=vci.me.UnscaledTime;bF=vci.me.Time;coroutine.yield(100)end;a.NillableIfHasValue(di,function(dp)dp()end)while true do local bJ=vci.me.Time;local dq=bJ-bF;local dn=vci.me.UnscaledTime;local dr=dn-dl;dh(dq,dr)bF=bJ;dl=dn;coroutine.yield(100)end end)end,\nCreateSlideSwitch=function(ds)local dt=a.NillableValue(ds.colliderItem)local du=a.NillableValue(ds.baseItem)local dv=a.NillableValue(ds.knobItem)local dw=a.NillableValueOrDefault(ds.minValue,0)local dx=a.NillableValueOrDefault(ds.maxValue,10)if dw>=dx then error(\'SlideSwitch: Invalid argument: minValue >= maxValue\',2)end;local dy=(dw+dx)*0.5;local dz=function(aD)local dA,dB=a.PingPong(aD-dw,dx-dw)return dA+dw,dB end;local q=dz(a.NillableValueOrDefault(ds.value,0))local dC=a.NillableIfHasValueOrElse(ds.tickFrequency,function(dD)if dD<=0 then error(\'SlideSwitch: Invalid argument: tickFrequency <= 0\',3)end;return math.min(dD,dx-dw)end,function()return(dx-dw)/10.0 end)local dE=a.NillableIfHasValueOrElse(ds.tickVector,function(b3)return Vector3.__new(b3.x,b3.y,b3.z)end,function()return Vector3.__new(0.01,0.0,0.0)end)local dF=dE.magnitude;if dF<Vector3.kEpsilon then error(\'SlideSwitch: Invalid argument: tickVector is too small\',2)end;local dG=a.NillableValueOrDefault(ds.snapToTick,true)local dH=ds.valueTextName;local dI=a.NillableValueOrDefault(ds.valueToText,tostring)local dJ=TimeSpan.FromMilliseconds(1000)local dK=TimeSpan.FromMilliseconds(50)local dL,dM;local cD={}local self;local dN=false;local dO=0;local dP=false;local dQ=TimeSpan.Zero;local dR=TimeSpan.Zero;local dS=function(dT,dU)if dU or dT~=q then local cE=q;q=dT;for cF,E in pairs(cD)do cF(self,q,cE)end end;dv.SetLocalPosition((dT-dy)/dC*dE)if dH then vci.assets.SetText(dH,dI(dT,self))end end;local dV=function()local dW=dL()local dX,dY=dz(dW)local dZ=dW+dC;local d_,e0=dz(dZ)assert(d_)local dT;if dY==e0 or dX==dx or dX==dw then dT=dZ else dT=dY>=0 and dx or dw end;dR=vci.me.UnscaledTime;if dT==dx or dT==dw then dQ=dR end;dM(dT)end;a.NillableIfHasValueOrElse(ds.lsp,function(e1)if not a.NillableHasValue(ds.propertyName)then error(\'SlideSwitch: Invalid argument: propertyName is nil\',3)end;local e2=a.NillableValue(ds.propertyName)dL=function()return e1.GetProperty(e2,q)end;dM=function(aD)e1.SetProperty(e2,aD)end;e1.AddListener(function(ar,z,e3,e4)if z==e2 then dS(dz(e3),true)end end)end,function()local e3=q;dL=function()return e3 end;dM=function(aD)e3=aD;dS(dz(aD),true)end end)self={GetColliderItem=function()return dt end,GetBaseItem=function()return du end,GetKnobItem=function()return dv end,GetMinValue=function()return dw end,GetMaxValue=function()return dx end,GetValue=function()return q end,GetScaleValue=function(e5,e6)assert(e5<=e6)return e5+(e6-e5)*(q-dw)/(dx-dw)end,SetValue=function(aD)dM(dz(aD))end,GetTickFrequency=function()return dC end,IsSnapToTick=function()return dG end,AddListener=function(cF)cD[cF]=cF end,RemoveListener=function(cF)cD[cF]=nil end,DoUse=function()if not dN then dP=true;dQ=vci.me.UnscaledTime;dV()end end,DoUnuse=function()dP=false end,DoGrab=function()if not dP then dN=true;dO=(q-dy)/dC end end,DoUngrab=function()dN=false end,Update=function()if dN then local e7=dt.GetPosition()-du.GetPosition()local e8=dv.GetRotation()*dE;local e9=Vector3.Project(e7,e8)local ea=(Vector3.Dot(e8,e9)>=0 and 1 or-1)*e9.magnitude/dF+dO;local eb=(dG and a.Round(ea)or ea)*dC+dy;local dT=a.Clamp(eb,dw,dx)if dT~=q then dM(dT)end elseif dP then local ec=vci.me.UnscaledTime;if ec>=dQ+dJ and ec>=dR+dK then dV()end elseif dt.IsMine then a.AlignSubItemOrigin(du,dt)end end}dS(dz(dL()),false)return self end,CreateSubItemConnector=function()local ed=function(ee,d2,ef)ee.item=d2;ee.position=d2.GetPosition()ee.rotation=d2.GetRotation()ee.initialPosition=ee.position;ee.initialRotation=ee.rotation;ee.propagation=not not ef;return ee end;local eg=function(eh)for d2,ee in pairs(eh)do ed(ee,d2,ee.propagation)end end;local ei=function(ej,bh,ee,ek,el)local e7=ej-ee.initialPosition;local em=bh*Quaternion.Inverse(ee.initialRotation)ee.position=ej;ee.rotation=bh;for d2,en in pairs(ek)do if d2~=ee.item and(not el or el(en))then en.position,en.rotation=a.RotateAround(en.initialPosition+e7,en.initialRotation,ej,em)d2.SetPosition(en.position)d2.SetRotation(en.rotation)end end end;local eo={}local ep=true;local eq=false;local self;self={IsEnabled=function()return ep end,SetEnabled=function(aF)ep=aF;if aF then eg(eo)eq=false end end,Contains=function(er)return a.NillableHasValue(eo[er])end,Add=function(es,et)if not es then error(\'SubItemConnector.Add: Invalid argument: subItems = \'..tostring(es),2)end;local eu=type(es)==\'table\'and es or{es}eg(eo)eq=false;for N,d2 in pairs(eu)do eo[d2]=ed({},d2,not et)end end,Remove=function(er)local aT=a.NillableHasValue(eo[er])eo[er]=nil;return aT end,RemoveAll=function()eo={}return true end,Each=function(ah)for d2,ee in pairs(eo)do if ah(d2,self)==false then return false end end end,GetItems=function()local eu={}for d2,ee in pairs(eo)do table.insert(eu,d2)end;return eu end,Update=function()if not ep then return end;local ev=false;for d2,ee in pairs(eo)do local ci=d2.GetPosition()local ew=d2.GetRotation()if not a.VectorApproximatelyEquals(ci,ee.position)or not a.QuaternionApproximatelyEquals(ew,ee.rotation)then if ee.propagation then if d2.IsMine then ei(ci,ew,eo[d2],eo,function(en)if en.item.IsMine then return true else eq=true;return false end end)ev=true;break else eq=true end else eq=true end end end;if not ev and eq then eg(eo)eq=false end end}return self end,GetSubItemTransform=function(er)local ej=er.GetPosition()local bh=er.GetRotation()local ex=er.GetLocalScale()return{positionX=ej.x,positionY=ej.y,positionZ=ej.z,rotationX=bh.x,rotationY=bh.y,rotationZ=bh.z,rotationW=bh.w,scaleX=ex.x,scaleY=ex.y,scaleZ=ex.z}end,RestoreCytanbTransform=function(ey)local ci=ey.positionX and ey.positionY and ey.positionZ and Vector3.__new(ey.positionX,ey.positionY,ey.positionZ)or nil;local ew=ey.rotationX and ey.rotationY and ey.rotationZ and ey.rotationW and Quaternion.__new(ey.rotationX,ey.rotationY,ey.rotationZ,ey.rotationW)or nil;local ex=ey.scaleX and ey.scaleY and ey.scaleZ and Vector3.__new(ey.scaleX,ey.scaleY,ey.scaleZ)or nil;return ci,ew,ex end}a.SetConstEach(a,{LogLevelOff=0,LogLevelFatal=100,LogLevelError=200,LogLevelWarn=300,LogLevelInfo=400,LogLevelDebug=500,LogLevelTrace=600,LogLevelAll=0x7FFFFFFF,ColorHueSamples=10,ColorSaturationSamples=4,ColorBrightnessSamples=5,EscapeSequenceTag=\'#__CYTANB\',SolidusTag=\'#__CYTANB_SOLIDUS\',NegativeNumberTag=\'#__CYTANB_NEGATIVE_NUMBER\',ArrayNumberTag=\'#__CYTANB_ARRAY_NUMBER\',InstanceIDParameterName=\'__CYTANB_INSTANCE_ID\',MessageValueParameterName=\'__CYTANB_MESSAGE_VALUE\',MessageSenderOverride=\'__CYTANB_MESSAGE_SENDER_OVERRIDE\',MessageOriginalSender=\'__CYTANB_MESSAGE_ORIGINAL_SENDER\',TypeParameterName=\'__CYTANB_TYPE\',ColorTypeName=\'Color\',Vector2TypeName=\'Vector2\',Vector3TypeName=\'Vector3\',Vector4TypeName=\'Vector4\',QuaternionTypeName=\'Quaternion\',LOCAL_SHARED_PROPERTY_EXPIRED_KEY=\'__CYTANB_LOCAL_SHARED_PROPERTY_EXPIRED\'})a.SetConstEach(a,{ColorMapSize=a.ColorHueSamples*a.ColorSaturationSamples*a.ColorBrightnessSamples,FatalLogLevel=a.LogLevelFatal,ErrorLogLevel=a.LogLevelError,WarnLogLevel=a.LogLevelWarn,InfoLogLevel=a.LogLevelInfo,DebugLogLevel=a.LogLevelDebug,TraceLogLevel=a.LogLevelTrace})c={[a.ColorTypeName]={compositionFieldNames=a.ListToMap({\'r\',\'g\',\'b\',\'a\'}),compositionFieldLength=4,toTableFunc=a.ColorToTable,fromTableFunc=a.ColorFromTable},[a.Vector2TypeName]={compositionFieldNames=a.ListToMap({\'x\',\'y\'}),compositionFieldLength=2,toTableFunc=a.Vector2ToTable,fromTableFunc=a.Vector2FromTable},[a.Vector3TypeName]={compositionFieldNames=a.ListToMap({\'x\',\'y\',\'z\'}),compositionFieldLength=3,toTableFunc=a.Vector3ToTable,fromTableFunc=a.Vector3FromTable},[a.Vector4TypeName]={compositionFieldNames=a.ListToMap({\'x\',\'y\',\'z\',\'w\'}),compositionFieldLength=4,toTableFunc=a.Vector4ToTable,fromTableFunc=a.Vector4FromTable},[a.QuaternionTypeName]={compositionFieldNames=a.ListToMap({\'x\',\'y\',\'z\',\'w\'}),compositionFieldLength=4,toTableFunc=a.QuaternionToTable,fromTableFunc=a.QuaternionFromTable}}d={{tag=a.NegativeNumberTag,pattern=\'^\'..a.NegativeNumberTag,replacement=\'\'},{tag=a.ArrayNumberTag,pattern=\'^\'..a.ArrayNumberTag,replacement=\'\'},{tag=a.SolidusTag,pattern=\'^\'..a.SolidusTag,replacement=\'/\'},{tag=a.EscapeSequenceTag,pattern=\'^\'..a.EscapeSequenceTag..a.EscapeSequenceTag,replacement=a.EscapeSequenceTag}}e=a.ListToMap({a.NegativeNumberTag,a.ArrayNumberTag})f=a.LogLevelInfo;h={[a.LogLevelFatal]=\'FATAL\',[a.LogLevelError]=\'ERROR\',[a.LogLevelWarn]=\'WARN\',[a.LogLevelInfo]=\'INFO\',[a.LogLevelDebug]=\'DEBUG\',[a.LogLevelTrace]=\'TRACE\'}package.loaded[\'cytanb\']=a;i,j=(function()local cw=\'eff3a188-bfc7-4b0e-93cb-90fd1adc508c\'local cC=_G[cw]if not cC then cC={}_G[cw]=cC end;local ez=cC.randomSeedValue;if not ez then ez=os.time()-os.clock()*10000;cC.randomSeedValue=ez;math.randomseed(ez)end;local eA=cC.clientID;if type(eA)~=\'string\'then eA=tostring(a.RandomUUID())cC.clientID=eA end;local eB=vci.state.Get(b)or\'\'if eB==\'\'and vci.assets.IsMine then eB=tostring(a.RandomUUID())vci.state.Set(b,eB)end;return eB,eA end)()return a end)()\n\nif vci.assets.IsMine then\n    cytanb.EmitCommentMessage(\'";
            s2 = "\', {name = \'（運営）\', commentSource = \'Nicolive\'})\nend";

            File.WriteAllText(targetPath, s1 + msg + s2);
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
            linkName = linkName.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
            linkName = linkName.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
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
            linkName = linkName.Replace("\n", "").Replace("$", "＄").Replace("/", "／").Replace(",", "，");
            linkName = linkName.Replace("\\\"", "”").Replace("\\\'", "’").Replace("\\", "＼");
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