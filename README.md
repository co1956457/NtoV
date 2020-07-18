<pre>
これは NCV プラグインです。
運営コメントを CommentBaton VCI に送ります。
これにより、縦書きコメビュに運営コメントが表示されるようになります。

NCV(Niconama Comment Viewer)はこちらから入手できます。
https://www.posite-c.com/application/ncv/
CommentBaton と縦書きコメビュはこちらから入手できます。
https://seed.online/users/100215#products

１．バーチャルキャストを立ち上げて CommentBaton と縦書きコメビュを出してください。

２．NtoV.dll を NCV の plugins フォルダに置いてください。
C:\Program Files (x86)\posite-c\NiconamaCommentViewer\plugins
※右クリック→プロパティ→セキュリティ:このファイルは…☑許可する(K)

３．テキストファイルを作成してください。
C:\Users\%ユーザー名%\AppData\Roaming\posite-c\NiconamaCommentViewer\NtoV.txt

４．NtoV.txt に CommentBaton VCI の場所を書いてください。
C:\Users\%ユーザー名%\AppData\LocalLow\infiniteloopCo,Ltd\VirtualCast\EmbeddedScriptWorkspace\CommentBaton

５．NCV を立ち上げてください。

※次回以降はNCVを先に起動することをおすすめします。
　起動時に main.lua が初期化されます。

※既知の問題
　CommentBaton を先に出現させた時に、前回最後のコメントが main.lua に残っていたらそれが流れます。
　コメントが大量にあるときなど、 NCV を接続した時に過去の運営コメが流れることがあります。
　読み込みのタイミングの問題ではないかと思います。

ライセンス： MIT


This is NCV plugin.
This plugin sends the control comments to CommentBaton VCI.
Then, Vertical Comment Viewer VCI would be able to get the control comments.

You can get NCV(Niconama Comment Viewer) from here.
https://www.posite-c.com/application/ncv/
You can get CommentBaton and Vertical Comment Viewer from here.
https://seed.online/users/100215#products

1. Please boot the VirtualCast, then cause to appear CommentBaton and Vertical Comment Viewer VCI.

2. Please put NtoV.dll in the NCV folder
C:\Program Files (x86)\posite-c\NiconamaCommentViewer\plugins
! Right click -> Properties -> please tick the checkbox named "Unblock".

3. Please create the text file.
C:\Users\%UserName%\AppData\Roaming\posite-c\NiconamaCommentViewer\NtoV.txt

4. Please write the CommentBaton VCI directory in the NtoV.txt.
C:\Users\%UserName%\AppData\LocalLow\infiniteloopCo,Ltd\VirtualCast\EmbeddedScriptWorkspace\CommentBaton

5. Please boot NCV.

! I recommend booting NCV first from the next time.
  When the plugin was booted, the main.lua would be initialized.

! Known Issues
 When CommentBaton VCI was appeared, the VCI sends the last comment if it was in main.lua.
 If there were a lot of comments, the VCI might sends a past comment.
 I'm not sure, however, I think that it depends on the reading timing.

License: MIT
</pre>
