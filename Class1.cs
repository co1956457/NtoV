// http://c-loft.com/blog/?p=719     この記事を参考に作成                              webの公開情報
// https://github.com/chinng-inta    運営コメントの条件分岐等参考にした                MIT License
// https://github.com/oocytanb       CommentBaton から縦書きコメビュにメッセージを送る MIT License
//
// SPDX-License-Identifier: MIT
// 20200718 v1.0 Taki co1956457
// 20200725 v2.0 タイマー方式に変更
//               cytanb を最新版に更新 (ver. Commits on Jul 24, 2020).
//
using System;
using System.IO;                    // File, Directory
using System.Collections.Generic;   // List
using System.Windows.Forms;         // MessageBox
using System.Text;                  // StringBuilder
using System.Timers;                // Timer

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
                    buffEmit.Add("    cytanb.EmitCommentMessage(\'" + comment + "\', {name = \'" + "（運営）" + "\', commentSource = \'" + "Nicolive" + "\'})");
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
                // cytanb ver. Commits on Jul 24, 2020
                // \ -> \\      ' -> \'     " -> \"
                s1 = "-- SPDX-License-Identifier: MIT\n-- Copyright (c) 2019 oO (https://github.com/oocytanb)\n---@type cytanb @See `cytanb_annotations.lua`\n\nlocal cytanb=(function()local b=\'__CYTANB_INSTANCE_ID\'local c;local d;local e;local f;local g;local h;local i;local j;local k=false;local l;local m;local n;local a;local o=function(p,q)local r=p+q-1;return p,r,r+1 end;local s=function(p,q)local r=p-q+1;return r,p,r-1 end;local t=function(u,v,w,p,x)local y=w.searchMap;for z,q in pairs(w.lengthList)do if q<=0 then error(\'SearchPattern: Invalid parameter: searchLen <= 0\')else local A,B,r=x(p,q)if A>=1 and B<=v then local C=string.sub(u,A,B)if y[C]then return true,q,r end end end end;return false,-1,-1 end;local D=function(u,w,p,x)if u==nil or w==nil then return false,-1 end;if w.hasEmptySearch then return true,0 end;local v=string.len(u)local E=w.repeatMin;local F=w.repeatMax;local G=F<0;local H=p;local I=0;local J=0;while G or J<F do local K,L,r=t(u,v,w,H,x)if K then if L<=0 then error(\'SearchPattern: Invalid parameter\')end;H=r;I=I+L;J=J+1 else break end end;if J>=E then return true,I else return false,-1 end end;local M=function(u,N,p,O,x,P)if u==nil or N==nil then return false,-1 end;local q=string.len(N)if O then local K=P(u,N)return K,K and q or-1 else if q==0 then return true,q end;local v=string.len(u)local A,B=x(p,q)if A>=1 and B<=v then local C=string.sub(u,A,B)local K=P(C,N)return K,K and q or-1 else return false,-1 end end end;local Q=function(R,S)for T=1,4 do local U=R[T]-S[T]if U~=0 then return U end end;return 0 end;local V;V={__eq=function(R,S)return R[1]==S[1]and R[2]==S[2]and R[3]==S[3]and R[4]==S[4]end,__lt=function(R,S)return Q(R,S)<0 end,__le=function(R,S)return Q(R,S)<=0 end,__tostring=function(W)local X=W[2]or 0;local Y=W[3]or 0;return string.format(\'%08x-%04x-%04x-%04x-%04x%08x\',bit32.band(W[1]or 0,0xFFFFFFFF),bit32.band(bit32.rshift(X,16),0xFFFF),bit32.band(X,0xFFFF),bit32.band(bit32.rshift(Y,16),0xFFFF),bit32.band(Y,0xFFFF),bit32.band(W[4]or 0,0xFFFFFFFF))end,__concat=function(R,S)local Z=getmetatable(R)local _=Z==V or type(Z)==\'table\'and Z.__concat==V.__concat;local a0=getmetatable(S)local a1=a0==V or type(a0)==\'table\'and a0.__concat==V.__concat;if not _ and not a1 then error(\'UUID: attempt to concatenate illegal values\',2)end;return(_ and V.__tostring(R)or R)..(a1 and V.__tostring(S)or S)end}local a2=\'__CYTANB_CONST_VARIABLES\'local a3=function(table,a4)local a5=getmetatable(table)if a5 then local a6=rawget(a5,a2)if a6 then local a7=rawget(a6,a4)if type(a7)==\'function\'then return a7(table,a4)else return a7 end end end;return nil end;local a8=function(table,a4,a9)local a5=getmetatable(table)if a5 then local a6=rawget(a5,a2)if a6 then if rawget(a6,a4)~=nil then error(\'Cannot assign to read only field \"\'..a4 ..\'\"\',2)end end end;rawset(table,a4,a9)end;local aa=function(ab,ac)local ad=ab[a.TypeParameterName]if a.NillableHasValue(ad)and a.NillableValue(ad)~=ac then return false,false end;return a.NillableIfHasValueOrElse(h[ac],function(ae)local af=ae.compositionFieldNames;local ag=ae.compositionFieldLength;local ah=false;for ai,a9 in pairs(ab)do if af[ai]then ag=ag-1;if ag<=0 and ah then break end elseif ai~=a.TypeParameterName then ah=true;if ag<=0 then break end end end;return ag<=0,ah end,function()return false,false end)end;local aj=function(u)return a.StringReplace(a.StringReplace(u,a.EscapeSequenceTag,a.EscapeSequenceTag..a.EscapeSequenceTag),\'/\',a.SolidusTag)end;local ak=function(u,al)local am=string.len(u)local an=string.len(a.EscapeSequenceTag)if an>am then return u end;local ao=\'\'local T=1;while T<am do local B,ap=string.find(u,a.EscapeSequenceTag,T,true)if not B then if T==1 then ao=u else ao=ao..string.sub(u,T)end;break end;if B>T then ao=ao..string.sub(u,T,B-1)end;local aq=false;for ar,as in ipairs(g)do local K=a.StringStartsWith(u,as.search,B)if K then ao=ao..(al and al(as.tag)or as.replacement)T=B+string.len(as.search)aq=true;break end end;if not aq then ao=ao..a.EscapeSequenceTag;T=ap+1 end end;return ao end;local at;at=function(au,av)if type(au)~=\'table\'then return au end;if not av then av={}end;if av[au]then error(\'circular reference\')end;av[au]=true;local aw={}for ai,a9 in pairs(au)do local ax=type(ai)local ay;if ax==\'string\'then ay=aj(ai)elseif ax==\'number\'then ay=tostring(ai)..a.ArrayNumberTag else ay=ai end;local az=type(a9)if az==\'string\'then aw[ay]=aj(a9)elseif az==\'number\'and a9<0 then aw[tostring(ay)..a.NegativeNumberTag]=tostring(a9)else aw[ay]=at(a9,av)end end;av[au]=nil;return aw end;local aA;aA=function(aw,aB)if type(aw)~=\'table\'then return aw end;local au={}for ai,a9 in pairs(aw)do local ay;local aC=false;if type(ai)==\'string\'then local aD=false;ay=ak(ai,function(aE)if aE==a.NegativeNumberTag then aC=true elseif aE==a.ArrayNumberTag then aD=true end;return nil end)if aD then ay=tonumber(ay)or ay end else ay=ai;aC=false end;if aC and type(a9)==\'string\'then au[ay]=tonumber(a9)elseif type(a9)==\'string\'then au[ay]=ak(a9,function(aE)return i[aE]end)else au[ay]=aA(a9,aB)end end;if not aB then a.NillableIfHasValue(au[a.TypeParameterName],function(aF)a.NillableIfHasValue(h[aF],function(ae)local aG,ah=ae.fromTableFunc(au)if not ah then a.NillableIfHasValue(aG,function(W)au=W end)end end)end)end;return au end;local aH={[\'nil\']=function(aI)return nil end,[\'number\']=function(aI)return tonumber(aI)end,[\'string\']=function(aI)return tostring(aI)end,[\'boolean\']=function(aI)if aI then return true else return false end end}local aJ=function(aI,aK)local aL=type(aI)if aL==aK then return aI else local aM=aH[aK]if aM then return aM(aI)else return nil end end end;local aN=function(aO,aP)if aP and type(aP)==\'table\'then local aQ={}for a4,aI in pairs(aO)do local aR=aP[a4]local aS;if aR==nil then aS=aI else local aT=aJ(aR,type(aI))if aT==nil then aS=aI else aS=aT end end;aQ[a4]=aS end;aQ[a.MessageOriginalSender]=aO;return aQ else return aO end end;local aU=function(aV,aW,aP,aX)local aY={type=aX,name=\'\',commentSource=\'\'}local aZ={[a.MessageValueParameterName]=tostring(aW),[a.MessageSenderOverride]=type(aP)==\'table\'and a.Extend(aY,aP,true)or aY}a.EmitMessage(aV,aZ)end;local a_=function(aV,b0,b1)local b2,b3=(function()local aM=function(aO,b4,aZ)local aW=tostring(aZ[a.MessageValueParameterName]or\'\')b1(aO,b0,aW)end;local b2=a.OnMessage(aV,aM)local b3=a.OnMessage(b0,aM)return b2,b3 end)()return{Off=function()b2.Off()b3.Off()end}end;a={InstanceID=function()if m==\'\'then m=vci.state.Get(b)or\'\'end;return m end,NillableHasValue=function(b5)return b5~=nil end,NillableValue=function(b5)if b5==nil then error(\'nillable: value is nil\',2)end;return b5 end,NillableValueOrDefault=function(b5,b6)if b5==nil then if b6==nil then error(\'nillable: defaultValue is nil\',2)end;return b6 else return b5 end end,NillableIfHasValue=function(b5,b1)if b5==nil then return nil else return b1(b5)end end,NillableIfHasValueOrElse=function(b5,b1,b7)if b5==nil then return b7()else return b1(b5)end end,MakeSearchPattern=function(b8,b9,ba)local E=b9 and math.floor(b9)or 1;if E<0 then error(\'SearchPattern: Invalid parameter: optRepeatMin < 0\')end;local F=ba and math.floor(ba)or E;if F>=0 and F<E then error(\'SearchPattern: Invalid parameter: repeatMax < repeatMin\')end;local bb=F==0;local y={}local bc={}local bd={}local be=0;for bf,bg in pairs(b8)do local q=string.len(bg)if q==0 then bb=true else y[bg]=q;if not bc[q]then bc[q]=true;be=be+1;bd[be]=q end end end;table.sort(bd,function(bh,K)return bh>K end)return{hasEmptySearch=bb,searchMap=y,lengthList=bd,repeatMin=E,repeatMax=F}end,StringStartsWith=function(u,N,bi)local H=bi and math.max(1,math.floor(bi))or 1;if type(N)==\'table\'then return D(u,N,H,o)else return M(u,N,H,H==1,o,string.startsWith)end end,StringEndsWith=function(u,N,bj)if u==nil then return false,-1 end;local v=string.len(u)local H=bj and math.min(v,math.floor(bj))or v;if type(N)==\'table\'then return D(u,N,H,s)else return M(u,N,H,H==v,s,string.endsWith)end end,StringTrimStart=function(u,bk)if u==nil or u==\'\'then return u end;local K,L=a.StringStartsWith(u,bk or c)if K and L>=1 then return string.sub(u,L+1)else return u end end,StringTrimEnd=function(u,bk)if u==nil or u==\'\'then return u end;local K,L=a.StringEndsWith(u,bk or c)if K and L>=1 then return string.sub(u,1,string.len(u)-L)else return u end end,StringTrim=function(u,bk)return a.StringTrimEnd(a.StringTrimStart(u,bk),bk)end,StringReplace=function(u,bl,bm)local bn;local am=string.len(u)if bl==\'\'then bn=bm;for T=1,am do bn=bn..string.sub(u,T,T)..bm end else bn=\'\'local T=1;while true do local A,B=string.find(u,bl,T,true)if A then bn=bn..string.sub(u,T,A-1)..bm;T=B+1;if T>am then break end else bn=T==1 and u or bn..string.sub(u,T)break end end end;return bn end,SetConst=function(bl,aV,W)if type(bl)~=\'table\'then error(\'Cannot set const to non-table target\',2)end;local bo=getmetatable(bl)local a5=bo or{}local bp=rawget(a5,a2)if rawget(bl,aV)~=nil then error(\'Non-const field \"\'..aV..\'\" already exists\',2)end;if not bp then bp={}rawset(a5,a2,bp)a5.__index=a3;a5.__newindex=a8 end;rawset(bp,aV,W)if not bo then setmetatable(bl,a5)end;return bl end,\nSetConstEach=function(bl,bq)for ai,a9 in pairs(bq)do a.SetConst(bl,ai,a9)end;return bl end,Extend=function(bl,br,bs,bt,av)if bl==br or type(bl)~=\'table\'or type(br)~=\'table\'then return bl end;if bs then if not av then av={}end;if av[br]then error(\'circular reference\')end;av[br]=true end;for ai,a9 in pairs(br)do if bs and type(a9)==\'table\'then local bu=bl[ai]bl[ai]=a.Extend(type(bu)==\'table\'and bu or{},a9,bs,bt,av)else bl[ai]=a9 end end;if not bt then local bv=getmetatable(br)if type(bv)==\'table\'then if bs then local bw=getmetatable(bl)setmetatable(bl,a.Extend(type(bw)==\'table\'and bw or{},bv,true))else setmetatable(bl,bv)end end end;if bs then av[br]=nil end;return bl end,Vars=function(a9,bx,by,av)local bz;if bx then bz=bx~=\'__NOLF\'else bx=\'  \'bz=true end;if not by then by=\'\'end;if not av then av={}end;local bA=type(a9)if bA==\'table\'then av[a9]=av[a9]and av[a9]+1 or 1;local bB=bz and by..bx or\'\'local u=\'(\'..tostring(a9)..\') {\'local bC=true;for a4,aI in pairs(a9)do if bC then bC=false else u=u..(bz and\',\'or\', \')end;if bz then u=u..\'\\n\'..bB end;if type(aI)==\'table\'and av[aI]and av[aI]>0 then u=u..a4 ..\' = (\'..tostring(aI)..\')\'else u=u..a4 ..\' = \'..a.Vars(aI,bx,bB,av)end end;if not bC and bz then u=u..\'\\n\'..by end;u=u..\'}\'av[a9]=av[a9]-1;if av[a9]<=0 then av[a9]=nil end;return u elseif bA==\'function\'or bA==\'thread\'or bA==\'userdata\'then return\'(\'..bA..\')\'elseif bA==\'string\'then return\'(\'..bA..\') \'..string.format(\'%q\',a9)else return\'(\'..bA..\') \'..tostring(a9)end end,GetLogLevel=function()return j end,SetLogLevel=function(bD)j=bD end,IsOutputLogLevelEnabled=function()return k end,SetOutputLogLevelEnabled=function(bE)k=not not bE end,Log=function(bD,...)if bD<=j then local bF=k and(l[bD]or\'LOG LEVEL \'..tostring(bD))..\' | \'or\'\'local bG=table.pack(...)if bG.n==1 then local a9=bG[1]if a9~=nil then local u=type(a9)==\'table\'and a.Vars(a9)or tostring(a9)print(k and bF..u or u)else print(bF)end else local u=bF;for T=1,bG.n do local a9=bG[T]if a9~=nil then u=u..(type(a9)==\'table\'and a.Vars(a9)or tostring(a9))end end;print(u)end end end,LogFatal=function(...)a.Log(a.LogLevelFatal,...)end,LogError=function(...)a.Log(a.LogLevelError,...)end,LogWarn=function(...)a.Log(a.LogLevelWarn,...)end,LogInfo=function(...)a.Log(a.LogLevelInfo,...)end,LogDebug=function(...)a.Log(a.LogLevelDebug,...)end,LogTrace=function(...)a.Log(a.LogLevelTrace,...)end,FatalLog=function(...)a.LogFatal(...)end,ErrorLog=function(...)a.LogError(...)end,WarnLog=function(...)a.LogWarn(...)end,InfoLog=function(...)a.LogInfo(...)end,DebugLog=function(...)a.LogDebug(...)end,TraceLog=function(...)a.LogTrace(...)end,ListToMap=function(bH,bI)local bJ={}if bI==nil then for ai,a9 in pairs(bH)do bJ[a9]=a9 end elseif type(bI)==\'function\'then for ai,a9 in pairs(bH)do local bK,bL=bI(a9)bJ[bK]=bL end else for ai,a9 in pairs(bH)do bJ[a9]=bI end end;return bJ end,Round=function(bM,bN)if bN then local bO=10^bN;return math.floor(bM*bO+0.5)/bO else return math.floor(bM+0.5)end end,Clamp=function(W,bP,bQ)return math.max(bP,math.min(W,bQ))end,Lerp=function(bh,K,bA)if bA<=0.0 then return bh elseif bA>=1.0 then return K else return bh+(K-bh)*bA end end,LerpUnclamped=function(bh,K,bA)if bA==0.0 then return bh elseif bA==1.0 then return K else return bh+(K-bh)*bA end end,PingPong=function(bA,bR)if bR==0 then return 0,1 end;local bS=math.floor(bA/bR)local bT=bA-bS*bR;if bS<0 then if(bS+1)%2==0 then return bR-bT,-1 else return bT,1 end else if bS%2==0 then return bT,1 else return bR-bT,-1 end end end,VectorApproximatelyEquals=function(bU,bV)return(bU-bV).sqrMagnitude<1E-10 end,QuaternionApproximatelyEquals=function(bU,bV)local bW=Quaternion.Dot(bU,bV)return bW<1.0+1E-06 and bW>1.0-1E-06 end,QuaternionToAngleAxis=function(bX)local bS=bX.normalized;local bY=math.acos(bS.w)local bZ=math.sin(bY)local b_=math.deg(bY*2.0)local c0;if math.abs(bZ)<=Quaternion.kEpsilon then c0=Vector3.right else local A=1.0/bZ;c0=Vector3.__new(bS.x*A,bS.y*A,bS.z*A)end;return b_,c0 end,QuaternionTwist=function(bX,c1)if c1.sqrMagnitude<Vector3.kEpsilonNormalSqrt then return Quaternion.identity end;local c2=Vector3.__new(bX.x,bX.y,bX.z)if c2.sqrMagnitude>=Vector3.kEpsilonNormalSqrt then local c3=Vector3.Project(c2,c1)if c3.sqrMagnitude>=Vector3.kEpsilonNormalSqrt then local c4=Quaternion.__new(c3.x,c3.y,c3.z,bX.w)c4.Normalize()return c4 else return Quaternion.AngleAxis(0,c1)end else local c5=a.QuaternionToAngleAxis(bX)return Quaternion.AngleAxis(c5,c1)end end,ApplyQuaternionToVector3=function(bX,c6)local c7=bX.w*c6.x+bX.y*c6.z-bX.z*c6.y;local c8=bX.w*c6.y-bX.x*c6.z+bX.z*c6.x;local c9=bX.w*c6.z+bX.x*c6.y-bX.y*c6.x;local ca=-bX.x*c6.x-bX.y*c6.y-bX.z*c6.z;return Vector3.__new(ca*-bX.x+c7*bX.w+c8*-bX.z-c9*-bX.y,ca*-bX.y-c7*-bX.z+c8*bX.w+c9*-bX.x,ca*-bX.z+c7*-bX.y-c8*-bX.x+c9*bX.w)end,RotateAround=function(cb,cc,cd,ce)return cd+ce*(cb-cd),ce*cc end,Random32=function()return bit32.band(math.random(-2147483648,2147483646),0xFFFFFFFF)end,RandomUUID=function()return a.UUIDFromNumbers(a.Random32(),bit32.bor(0x4000,bit32.band(a.Random32(),0xFFFF0FFF)),bit32.bor(0x80000000,bit32.band(a.Random32(),0x3FFFFFFF)),a.Random32())end,UUIDString=function(cf)return V.__tostring(cf)end,UUIDFromNumbers=function(...)local cg=...local bA=type(cg)local ch,ci,cj,ck;if bA==\'table\'then ch=cg[1]ci=cg[2]cj=cg[3]ck=cg[4]else ch,ci,cj,ck=...end;local cf={bit32.band(ch or 0,0xFFFFFFFF),bit32.band(ci or 0,0xFFFFFFFF),bit32.band(cj or 0,0xFFFFFFFF),bit32.band(ck or 0,0xFFFFFFFF)}setmetatable(cf,V)return cf end,UUIDFromString=function(u)local am=string.len(u)if am==32 then local cf=a.UUIDFromNumbers(0,0,0,0)for T=1,4 do local A=1+(T-1)*8;if not a.StringStartsWith(u,e,A)then return nil end;cf[T]=tonumber(string.sub(u,A,A+7),16)end;return cf elseif am==36 then if not a.StringStartsWith(u,e,1)then return nil end;local ch=tonumber(string.sub(u,1,8),16)if not a.StringStartsWith(u,\'-\',9)or not a.StringStartsWith(u,d,10)or not a.StringStartsWith(u,\'-\',14)or not a.StringStartsWith(u,d,15)then return nil end;local ci=tonumber(string.sub(u,10,13)..string.sub(u,15,18),16)if not a.StringStartsWith(u,\'-\',19)or not a.StringStartsWith(u,d,20)or not a.StringStartsWith(u,\'-\',24)or not a.StringStartsWith(u,d,25)then return nil end;local cj=tonumber(string.sub(u,20,23)..string.sub(u,25,28),16)if not a.StringStartsWith(u,e,29)then return nil end;local ck=tonumber(string.sub(u,29),16)return a.UUIDFromNumbers(ch,ci,cj,ck)else return nil end end,ParseUUID=function(u)return a.UUIDFromString(u)end,CreateCircularQueue=function(cl)if type(cl)~=\'number\'or cl<1 then error(\'CreateCircularQueue: Invalid argument: capacity = \'..tostring(cl),2)end;local self;local cm=math.floor(cl)local ao={}local cn=0;local co=0;local cp=0;self={Size=function()return cp end,Clear=function()cn=0;co=0;cp=0 end,IsEmpty=function()return cp==0 end,Offer=function(cq)ao[cn+1]=cq;cn=(cn+1)%cm;if cp<cm then cp=cp+1 else co=(co+1)%cm end;return true end,OfferFirst=function(cq)co=(cm+co-1)%cm;ao[co+1]=cq;if cp<cm then cp=cp+1 else cn=(cm+cn-1)%cm end;return true end,Poll=function()if cp==0 then return nil else local cq=ao[co+1]co=(co+1)%cm;cp=cp-1;return cq end end,PollLast=function()if cp==0 then return nil else cn=(cm+cn-1)%cm;local cq=ao[cn+1]cp=cp-1;return cq end end,Peek=function()if cp==0 then return nil else return ao[co+1]end end,PeekLast=function()if cp==0 then return nil else return ao[(cm+cn-1)%cm+1]end end,Get=function(cr)if cr<1 or cr>cp then a.LogError(\'CreateCircularQueue.Get: index is outside the range: \'..cr)return nil end;return ao[(co+cr-1)%cm+1]end,IsFull=function()return cp>=cm end,MaxSize=function()return cm end}return self end,DetectClicks=function(cs,ct,cu)local cv=cs or 0;local cw=cu or TimeSpan.FromMilliseconds(500)local cx=vci.me.Time;local cy=ct and cx>ct+cw and 1 or cv+1;return cy,cx end,ColorRGBToHSV=function(cz)local bT=math.max(0.0,math.min(cz.r,1.0))local cA=math.max(0.0,math.min(cz.g,1.0))local K=math.max(0.0,math.min(cz.b,1.0))local bQ=math.max(bT,cA,K)local bP=math.min(bT,cA,K)local cB=bQ-bP;local a7;if cB==0.0 then a7=0.0 elseif bQ==bT then a7=(cA-K)/cB/6.0 elseif bQ==cA then a7=(2.0+(K-bT)/cB)/6.0 else a7=(4.0+(bT-cA)/cB)/6.0 end;if a7<0.0 then a7=a7+1.0 end;local cC=bQ==0.0 and cB or cB/bQ;local a9=bQ;return a7,cC,a9 end,ColorFromARGB32=function(cD)local cE=type(cD)==\'number\'and cD or 0xFF000000;return Color.__new(bit32.band(bit32.rshift(cE,16),0xFF)/0xFF,bit32.band(bit32.rshift(cE,8),0xFF)/0xFF,bit32.band(cE,0xFF)/0xFF,bit32.band(bit32.rshift(cE,24),0xFF)/0xFF)end,ColorToARGB32=function(cz)return bit32.bor(bit32.lshift(bit32.band(a.Round(0xFF*cz.a),0xFF),24),bit32.lshift(bit32.band(a.Round(0xFF*cz.r),0xFF),16),bit32.lshift(bit32.band(a.Round(0xFF*cz.g),0xFF),8),bit32.band(a.Round(0xFF*cz.b),0xFF))end,ColorFromIndex=function(cF,cG,cH,cI,cJ)local cK=math.max(math.floor(cG or a.ColorHueSamples),1)local cL=cJ and cK or cK-1;local cM=math.max(math.floor(cH or a.ColorSaturationSamples),1)local cN=math.max(math.floor(cI or a.ColorBrightnessSamples),1)local cr=a.Clamp(math.floor(cF or 0),0,cK*cM*cN-1)local cO=cr%cK;local cP=math.floor(cr/cK)local A=cP%cM;local cQ=math.floor(cP/cM)if cJ or cO~=cL then local a7=cO/cL;local cC=(cM-A)/cM;local a9=(cN-cQ)/cN;return Color.HSVToRGB(a7,cC,a9)else local a9=(cN-cQ)/cN*A/(cM-1)return Color.HSVToRGB(0.0,0.0,a9)end end,\nColorToIndex=function(cz,cG,cH,cI,cJ)local cK=math.max(math.floor(cG or a.ColorHueSamples),1)local cL=cJ and cK or cK-1;local cM=math.max(math.floor(cH or a.ColorSaturationSamples),1)local cN=math.max(math.floor(cI or a.ColorBrightnessSamples),1)local a7,cC,a9=a.ColorRGBToHSV(cz)local A=a.Round(cM*(1.0-cC))if cJ or A<cM then local cR=a.Round(cL*a7)if cR>=cL then cR=0 end;if A>=cM then A=cM-1 end;local cQ=math.min(cN-1,a.Round(cN*(1.0-a9)))return cR+cK*(A+cM*cQ)else local cS=a.Round((cM-1)*a9)if cS==0 then local cT=a.Round(cN*(1.0-a9))if cT>=cN then return cK-1 else return cK*(1+a.Round(a9*(cM-1)/(cN-cT)*cN)+cM*cT)-1 end else return cK*(1+cS+cM*a.Round(cN*(1.0-a9*(cM-1)/cS)))-1 end end end,ColorToTable=function(cz)return{[a.TypeParameterName]=a.ColorTypeName,r=cz.r,g=cz.g,b=cz.b,a=cz.a}end,ColorFromTable=function(ab)local K,ah=aa(ab,a.ColorTypeName)return K and Color.__new(ab.r,ab.g,ab.b,ab.a)or nil,ah end,Vector2ToTable=function(W)return{[a.TypeParameterName]=a.Vector2TypeName,x=W.x,y=W.y}end,Vector2FromTable=function(ab)local K,ah=aa(ab,a.Vector2TypeName)return K and Vector2.__new(ab.x,ab.y)or nil,ah end,Vector3ToTable=function(W)return{[a.TypeParameterName]=a.Vector3TypeName,x=W.x,y=W.y,z=W.z}end,Vector3FromTable=function(ab)local K,ah=aa(ab,a.Vector3TypeName)return K and Vector3.__new(ab.x,ab.y,ab.z)or nil,ah end,Vector4ToTable=function(W)return{[a.TypeParameterName]=a.Vector4TypeName,x=W.x,y=W.y,z=W.z,w=W.w}end,Vector4FromTable=function(ab)local K,ah=aa(ab,a.Vector4TypeName)return K and Vector4.__new(ab.x,ab.y,ab.z,ab.w)or nil,ah end,QuaternionToTable=function(W)return{[a.TypeParameterName]=a.QuaternionTypeName,x=W.x,y=W.y,z=W.z,w=W.w}end,QuaternionFromTable=function(ab)local K,ah=aa(ab,a.QuaternionTypeName)return K and Quaternion.__new(ab.x,ab.y,ab.z,ab.w)or nil,ah end,TableToSerializable=function(au)return at(au)end,TableFromSerializable=function(aw,aB)return aA(aw,aB)end,TableToSerialiable=function(au)return at(au)end,TableFromSerialiable=function(aw,aB)return aA(aw,aB)end,EmitMessage=function(aV,aZ)local aw=a.NillableIfHasValueOrElse(aZ,function(au)if type(au)~=\'table\'then error(\'EmitMessage: Invalid argument: table expected\',3)end;return a.TableToSerializable(au)end,function()return{}end)aw[a.InstanceIDParameterName]=a.InstanceID()vci.message.Emit(aV,json.serialize(aw))end,OnMessage=function(aV,b1)local aM=function(aO,b4,aW)if type(aW)==\'string\'and string.startsWith(aW,\'{\')then local cU,aw=pcall(json.parse,aW)if cU and type(aw)==\'table\'and aw[a.InstanceIDParameterName]then local cV=a.TableFromSerializable(aw)b1(aN(aO,cV[a.MessageSenderOverride]),b4,cV)return end end;b1(aO,b4,{[a.MessageValueParameterName]=aW})end;vci.message.On(aV,aM)return{Off=function()if aM then aM=nil end end}end,OnInstanceMessage=function(aV,b1)local aM=function(aO,b4,aZ)local cW=a.InstanceID()if cW~=\'\'and cW==aZ[a.InstanceIDParameterName]then b1(aO,b4,aZ)end end;return a.OnMessage(aV,aM)end,EmitCommentMessage=function(aW,aP)aU(a.DedicatedCommentMessageName,aW,aP,\'comment\')end,OnCommentMessage=function(b1)a_(a.DedicatedCommentMessageName,\'comment\',b1)end,EmitNotificationMessage=function(aW,aP)aU(a.DedicatedNotificationMessageName,aW,aP,\'notification\')end,OnNotificationMessage=function(b1)a_(a.DedicatedNotificationMessageName,\'notification\',b1)end,GetEffekseerEmitterMap=function(aV)local cX=vci.assets.GetEffekseerEmitters(aV)if not cX then return nil end;local bJ={}for T,cY in pairs(cX)do bJ[cY.EffectName]=cY end;return bJ end,ClientID=function()return n end,ParseTagString=function(u)local cZ=string.find(u,\'#\',1,true)if not cZ then return{},u end;local c_={}local d0=string.sub(u,1,cZ-1)cZ=cZ+1;local am=string.len(u)while cZ<=am do local d1,d2=a.StringStartsWith(u,f,cZ)if d1 then local d3=cZ+d2;local d4=string.sub(u,cZ,d3-1)local d5=d4;cZ=d3;if cZ<=am and a.StringStartsWith(u,\'=\',cZ)then cZ=cZ+1;local d6,d7=a.StringStartsWith(u,f,cZ)if d6 then local d8=cZ+d7;d5=string.sub(u,cZ,d8-1)cZ=d8 else d5=\'\'end end;c_[d4]=d5 end;cZ=string.find(u,\'#\',cZ,true)if not cZ then break end;cZ=cZ+1 end;return c_,d0 end,CalculateSIPrefix=(function()local d9=9;local da={\'y\',\'z\',\'a\',\'f\',\'p\',\'n\',\'u\',\'m\',\'\',\'k\',\'M\',\'G\',\'T\',\'P\',\'E\',\'Z\',\'Y\'}local db=#da;return function(bM)local dc=bM==0 and 0 or a.Clamp(math.floor(math.log(math.abs(bM),1000)),1-d9,db-d9)return dc==0 and bM or bM/1000^dc,da[d9+dc],dc*3 end end)(),CreateLocalSharedProperties=function(dd,de)local df=TimeSpan.FromSeconds(5)local dg=\'33657f0e-7c44-4ee7-acd9-92dd8b8d807a\'local dh=\'__CYTANB_LOCAL_SHARED_PROPERTIES_LISTENER_MAP\'if type(dd)~=\'string\'or string.len(dd)<=0 or type(de)~=\'string\'or string.len(de)<=0 then error(\'LocalSharedProperties: Invalid arguments\',2)end;local di=_G[dg]if not di then di={}_G[dg]=di end;di[de]=vci.me.UnscaledTime;local dj=_G[dd]if not dj then dj={[dh]={}}_G[dd]=dj end;local dk=dj[dh]local self;self={GetLspID=function()return dd end,GetLoadID=function()return de end,GetProperty=function(a4,b6)local W=dj[a4]if W==nil then return b6 else return W end end,SetProperty=function(a4,W)if a4==dh then error(\'LocalSharedProperties: Invalid argument: key = \',a4,2)end;local cx=vci.me.UnscaledTime;local dl=dj[a4]dj[a4]=W;for dm,cW in pairs(dk)do local bA=di[cW]if bA and bA+df>=cx then dm(self,a4,W,dl)else dm(self,a.LOCAL_SHARED_PROPERTY_EXPIRED_KEY,true,false)dk[dm]=nil;di[cW]=nil end end end,Clear=function()for a4,W in pairs(dj)do if a4~=dh then self.SetProperty(a4,nil)end end end,Each=function(b1)for a4,W in pairs(dj)do if a4~=dh and b1(W,a4,self)==false then return false end end end,AddListener=function(dm)dk[dm]=de end,RemoveListener=function(dm)dk[dm]=nil end,UpdateAlive=function()di[de]=vci.me.UnscaledTime end}return self end,EstimateFixedTimestep=function(dn)local dp=1.0;local dq=1000.0;local dr=TimeSpan.FromSeconds(0.02)local ds=0xFFFF;local dt=a.CreateCircularQueue(64)local du=TimeSpan.FromSeconds(5)local dv=TimeSpan.FromSeconds(30)local dw=false;local dx=vci.me.Time;local dy=a.Random32()local dz=Vector3.__new(bit32.bor(0x400,bit32.band(dy,0x1FFF)),bit32.bor(0x400,bit32.band(bit32.rshift(dy,16),0x1FFF)),0.0)dn.SetPosition(dz)dn.SetRotation(Quaternion.identity)dn.SetVelocity(Vector3.zero)dn.SetAngularVelocity(Vector3.zero)dn.AddForce(Vector3.__new(0.0,0.0,dp*dq))local self={Timestep=function()return dr end,Precision=function()return ds end,IsFinished=function()return dw end,Update=function()if dw then return dr end;local dA=vci.me.Time-dx;local dB=dA.TotalSeconds;if dB<=Vector3.kEpsilon then return dr end;local dC=dn.GetPosition().z-dz.z;local dD=dC/dB;local dE=dD/dq;if dE<=Vector3.kEpsilon then return dr end;dt.Offer(dE)local dF=dt.Size()if dF>=2 and dA>=du then local dG=0.0;for T=1,dF do dG=dG+dt.Get(T)end;local dH=dG/dF;local dI=0.0;for T=1,dF do dI=dI+(dt.Get(T)-dH)^2 end;local dJ=dI/dF;if dJ<ds then ds=dJ;dr=TimeSpan.FromSeconds(dH)end;if dA>dv then dw=true;dn.SetPosition(dz)dn.SetRotation(Quaternion.identity)dn.SetVelocity(Vector3.zero)dn.SetAngularVelocity(Vector3.zero)end else dr=TimeSpan.FromSeconds(dE)end;return dr end}return self end,AlignSubItemOrigin=function(dK,dL,dM)local dN=dK.GetRotation()if not a.QuaternionApproximatelyEquals(dL.GetRotation(),dN)then dL.SetRotation(dN)end;local dO=dK.GetPosition()if not a.VectorApproximatelyEquals(dL.GetPosition(),dO)then dL.SetPosition(dO)end;if dM then dL.SetVelocity(Vector3.zero)dL.SetAngularVelocity(Vector3.zero)end end,CreateSubItemGlue=function()local dP={}local self;self={Contains=function(dQ,dR)return a.NillableIfHasValueOrElse(dP[dQ],function(bq)return a.NillableHasValue(bq[dR])end,function()return false end)end,Add=function(dQ,dS,dM)if not dQ or not dS then local dT=\'SubItemGlue.Add: Invalid arguments \'..(not dQ and\', parent = \'..tostring(dQ)or\'\')..(not dS and\', children = \'..tostring(dS)or\'\')error(dT,2)end;local bq=a.NillableIfHasValueOrElse(dP[dQ],function(dU)return dU end,function()local dU={}dP[dQ]=dU;return dU end)if type(dS)==\'table\'then for a4,aI in pairs(dS)do bq[aI]={velocityReset=not not dM}end else bq[dS]={velocityReset=not not dM}end end,Remove=function(dQ,dR)return a.NillableIfHasValueOrElse(dP[dQ],function(bq)if a.NillableHasValue(bq[dR])then bq[dR]=nil;return true else return false end end,function()return false end)end,RemoveParent=function(dQ)if a.NillableHasValue(dP[dQ])then dP[dQ]=nil;return true else return false end end,RemoveAll=function()dP={}return true end,Each=function(b1,dV)return a.NillableIfHasValueOrElse(dV,function(dQ)return a.NillableIfHasValue(dP[dQ],function(bq)for dR,dW in pairs(bq)do if b1(dR,dQ,self)==false then return false end end end)end,function()for dQ,bq in pairs(dP)do if self.Each(b1,dQ)==false then return false end end end)end,Update=function(dX)for dQ,bq in pairs(dP)do local dY=dQ.GetPosition()local dZ=dQ.GetRotation()for dR,dW in pairs(bq)do if dX or dR.IsMine then if not a.QuaternionApproximatelyEquals(dR.GetRotation(),dZ)then dR.SetRotation(dZ)end;if not a.VectorApproximatelyEquals(dR.GetPosition(),dY)then dR.SetPosition(dY)end;if dW.velocityReset then dR.SetVelocity(Vector3.zero)dR.SetAngularVelocity(Vector3.zero)end end end end end}return self end,\nCreateUpdateRoutine=function(d_,e0)return coroutine.wrap(function()local e1=TimeSpan.FromSeconds(30)local e2=vci.me.UnscaledTime;local e3=e2;local ct=vci.me.Time;local e4=true;while true do local cW=a.InstanceID()if cW~=\'\'then break end;local e5=vci.me.UnscaledTime;if e5-e1>e2 then a.LogError(\'TIMEOUT: Could not receive Instance ID.\')return-1 end;e3=e5;ct=vci.me.Time;e4=false;coroutine.yield(100)end;if e4 then e3=vci.me.UnscaledTime;ct=vci.me.Time;coroutine.yield(100)end;a.NillableIfHasValue(e0,function(e6)e6()end)while true do local cx=vci.me.Time;local e7=cx-ct;local e5=vci.me.UnscaledTime;local e8=e5-e3;d_(e7,e8)ct=cx;e3=e5;coroutine.yield(100)end end)end,CreateSlideSwitch=function(e9)local ea=a.NillableValue(e9.colliderItem)local eb=a.NillableValue(e9.baseItem)local ec=a.NillableValue(e9.knobItem)local ed=a.NillableValueOrDefault(e9.minValue,0)local ee=a.NillableValueOrDefault(e9.maxValue,10)if ed>=ee then error(\'SlideSwitch: Invalid argument: minValue >= maxValue\',2)end;local ef=(ed+ee)*0.5;local eg=function(aI)local eh,ei=a.PingPong(aI-ed,ee-ed)return eh+ed,ei end;local W=eg(a.NillableValueOrDefault(e9.value,0))local ej=a.NillableIfHasValueOrElse(e9.tickFrequency,function(ek)if ek<=0 then error(\'SlideSwitch: Invalid argument: tickFrequency <= 0\',3)end;return math.min(ek,ee-ed)end,function()return(ee-ed)/10.0 end)local el=a.NillableIfHasValueOrElse(e9.tickVector,function(c0)return Vector3.__new(c0.x,c0.y,c0.z)end,function()return Vector3.__new(0.01,0.0,0.0)end)local em=el.magnitude;if em<Vector3.kEpsilon then error(\'SlideSwitch: Invalid argument: tickVector is too small\',2)end;local en=a.NillableValueOrDefault(e9.snapToTick,true)local eo=e9.valueTextName;local ep=a.NillableValueOrDefault(e9.valueToText,tostring)local eq=TimeSpan.FromMilliseconds(1000)local er=TimeSpan.FromMilliseconds(50)local es,et;local dk={}local self;local eu=false;local ev=0;local ew=false;local ex=TimeSpan.Zero;local ey=TimeSpan.Zero;local ez=function(eA,eB)if eB or eA~=W then local dl=W;W=eA;for dm,a9 in pairs(dk)do dm(self,W,dl)end end;ec.SetLocalPosition((eA-ef)/ej*el)if eo then vci.assets.SetText(eo,ep(eA,self))end end;local eC=function()local eD=es()local eE,eF=eg(eD)local eG=eD+ej;local eH,eI=eg(eG)assert(eH)local eA;if eF==eI or eE==ee or eE==ed then eA=eG else eA=eF>=0 and ee or ed end;ey=vci.me.UnscaledTime;if eA==ee or eA==ed then ex=ey end;et(eA)end;a.NillableIfHasValueOrElse(e9.lsp,function(eJ)if not a.NillableHasValue(e9.propertyName)then error(\'SlideSwitch: Invalid argument: propertyName is nil\',3)end;local eK=a.NillableValue(e9.propertyName)es=function()return eJ.GetProperty(eK,W)end;et=function(aI)eJ.SetProperty(eK,aI)end;eJ.AddListener(function(br,a4,eL,eM)if a4==eK then ez(eg(eL),true)end end)end,function()local eL=W;es=function()return eL end;et=function(aI)eL=aI;ez(eg(aI),true)end end)self={GetColliderItem=function()return ea end,GetBaseItem=function()return eb end,GetKnobItem=function()return ec end,GetMinValue=function()return ed end,GetMaxValue=function()return ee end,GetValue=function()return W end,GetScaleValue=function(eN,eO)assert(eN<=eO)return eN+(eO-eN)*(W-ed)/(ee-ed)end,SetValue=function(aI)et(eg(aI))end,GetTickFrequency=function()return ej end,IsSnapToTick=function()return en end,AddListener=function(dm)dk[dm]=dm end,RemoveListener=function(dm)dk[dm]=nil end,DoUse=function()if not eu then ew=true;ex=vci.me.UnscaledTime;eC()end end,DoUnuse=function()ew=false end,DoGrab=function()if not ew then eu=true;ev=(W-ef)/ej end end,DoUngrab=function()eu=false end,Update=function()if eu then local eP=ea.GetPosition()-eb.GetPosition()local eQ=ec.GetRotation()*el;local eR=Vector3.Project(eP,eQ)local eS=(Vector3.Dot(eQ,eR)>=0 and 1 or-1)*eR.magnitude/em+ev;local eT=(en and a.Round(eS)or eS)*ej+ef;local eA=a.Clamp(eT,ed,ee)if eA~=W then et(eA)end elseif ew then local eU=vci.me.UnscaledTime;if eU>=ex+eq and eU>=ey+er then eC()end elseif ea.IsMine then a.AlignSubItemOrigin(eb,ea)end end}ez(eg(es()),false)return self end,CreateSubItemConnector=function()local eV=function(eW,dL,eX)eW.item=dL;eW.position=dL.GetPosition()eW.rotation=dL.GetRotation()eW.initialPosition=eW.position;eW.initialRotation=eW.rotation;eW.propagation=not not eX;return eW end;local eY=function(eZ)for dL,eW in pairs(eZ)do eV(eW,dL,eW.propagation)end end;local e_=function(p,ce,eW,f0,f1)local eP=p-eW.initialPosition;local f2=ce*Quaternion.Inverse(eW.initialRotation)eW.position=p;eW.rotation=ce;for dL,f3 in pairs(f0)do if dL~=eW.item and(not f1 or f1(f3))then f3.position,f3.rotation=a.RotateAround(f3.initialPosition+eP,f3.initialRotation,p,f2)dL.SetPosition(f3.position)dL.SetRotation(f3.rotation)end end end;local f4={}local f5=true;local f6=false;local self;self={IsEnabled=function()return f5 end,SetEnabled=function(bE)f5=bE;if bE then eY(f4)f6=false end end,Contains=function(f7)return a.NillableHasValue(f4[f7])end,Add=function(f8,f9)if not f8 then error(\'SubItemConnector.Add: Invalid argument: subItems = \'..tostring(f8),2)end;local fa=type(f8)==\'table\'and f8 or{f8}eY(f4)f6=false;for ai,dL in pairs(fa)do f4[dL]=eV({},dL,not f9)end end,Remove=function(f7)local K=a.NillableHasValue(f4[f7])f4[f7]=nil;return K end,RemoveAll=function()f4={}return true end,Each=function(b1)for dL,eW in pairs(f4)do if b1(dL,self)==false then return false end end end,GetItems=function()local fa={}for dL,eW in pairs(f4)do table.insert(fa,dL)end;return fa end,Update=function()if not f5 then return end;local fb=false;for dL,eW in pairs(f4)do local cZ=dL.GetPosition()local fc=dL.GetRotation()if not a.VectorApproximatelyEquals(cZ,eW.position)or not a.QuaternionApproximatelyEquals(fc,eW.rotation)then if eW.propagation then if dL.IsMine then e_(cZ,fc,f4[dL],f4,function(f3)if f3.item.IsMine then return true else f6=true;return false end end)fb=true;break else f6=true end else f6=true end end end;if not fb and f6 then eY(f4)f6=false end end}return self end,GetSubItemTransform=function(f7)local p=f7.GetPosition()local ce=f7.GetRotation()local fd=f7.GetLocalScale()return{positionX=p.x,positionY=p.y,positionZ=p.z,rotationX=ce.x,rotationY=ce.y,rotationZ=ce.z,rotationW=ce.w,scaleX=fd.x,scaleY=fd.y,scaleZ=fd.z}end}a.SetConstEach(a,{LogLevelOff=0,LogLevelFatal=100,LogLevelError=200,LogLevelWarn=300,LogLevelInfo=400,LogLevelDebug=500,LogLevelTrace=600,LogLevelAll=0x7FFFFFFF,ColorHueSamples=10,ColorSaturationSamples=4,ColorBrightnessSamples=5,EscapeSequenceTag=\'#__CYTANB\',SolidusTag=\'#__CYTANB_SOLIDUS\',NegativeNumberTag=\'#__CYTANB_NEGATIVE_NUMBER\',ArrayNumberTag=\'#__CYTANB_ARRAY_NUMBER\',InstanceIDParameterName=\'__CYTANB_INSTANCE_ID\',MessageValueParameterName=\'__CYTANB_MESSAGE_VALUE\',MessageSenderOverride=\'__CYTANB_MESSAGE_SENDER_OVERRIDE\',MessageOriginalSender=\'__CYTANB_MESSAGE_ORIGINAL_SENDER\',TypeParameterName=\'__CYTANB_TYPE\',ColorTypeName=\'Color\',Vector2TypeName=\'Vector2\',Vector3TypeName=\'Vector3\',Vector4TypeName=\'Vector4\',QuaternionTypeName=\'Quaternion\',DedicatedCommentMessageName=\'cytanb.comment.a2a6a035-6b8d-4e06-b4f9-07e6209b0639\',DedicatedNotificationMessageName=\'cytanb.notification.698ba55f-2b69-47f2-a68d-bc303994cff3\',LOCAL_SHARED_PROPERTY_EXPIRED_KEY=\'__CYTANB_LOCAL_SHARED_PROPERTY_EXPIRED\'})a.SetConstEach(a,{ColorMapSize=a.ColorHueSamples*a.ColorSaturationSamples*a.ColorBrightnessSamples,FatalLogLevel=a.LogLevelFatal,ErrorLogLevel=a.LogLevelError,WarnLogLevel=a.LogLevelWarn,InfoLogLevel=a.LogLevelInfo,DebugLogLevel=a.LogLevelDebug,TraceLogLevel=a.LogLevelTrace})c=a.MakeSearchPattern({\'\\t\',\'\\n\',\'\\v\',\'\\f\',\'\\r\',\' \'},1,-1)d,e=(function()local fe={\'A\',\'B\',\'C\',\'D\',\'E\',\'F\',\'a\',\'b\',\'c\',\'d\',\'e\',\'f\',\'0\',\'1\',\'2\',\'3\',\'4\',\'5\',\'6\',\'7\',\'8\',\'9\'}return a.MakeSearchPattern(fe,4,4),a.MakeSearchPattern(fe,8,8)end)()f=a.MakeSearchPattern({\'A\',\'B\',\'C\',\'D\',\'E\',\'F\',\'G\',\'H\',\'I\',\'J\',\'K\',\'L\',\'M\',\'N\',\'O\',\'P\',\'Q\',\'R\',\'S\',\'T\',\'U\',\'V\',\'W\',\'X\',\'Y\',\'Z\',\'a\',\'b\',\'c\',\'d\',\'e\',\'f\',\'g\',\'h\',\'i\',\'j\',\'k\',\'l\',\'m\',\'n\',\'o\',\'p\',\'q\',\'r\',\'s\',\'t\',\'u\',\'v\',\'w\',\'x\',\'y\',\'z\',\'0\',\'1\',\'2\',\'3\',\'4\',\'5\',\'6\',\'7\',\'8\',\'9\',\'_\',\'-\',\'.\',\'(\',\')\',\'!\',\'~\',\'*\',\'\\\'\',\'%\'},1,-1)g={{tag=a.NegativeNumberTag,search=a.NegativeNumberTag,replacement=\'\'},{tag=a.ArrayNumberTag,search=a.ArrayNumberTag,replacement=\'\'},{tag=a.SolidusTag,search=a.SolidusTag,replacement=\'/\'},{tag=a.EscapeSequenceTag,search=a.EscapeSequenceTag..a.EscapeSequenceTag,replacement=a.EscapeSequenceTag}}h={[a.ColorTypeName]={compositionFieldNames=a.ListToMap({\'r\',\'g\',\'b\',\'a\'}),compositionFieldLength=4,toTableFunc=a.ColorToTable,fromTableFunc=a.ColorFromTable},[a.Vector2TypeName]={compositionFieldNames=a.ListToMap({\'x\',\'y\'}),compositionFieldLength=2,toTableFunc=a.Vector2ToTable,fromTableFunc=a.Vector2FromTable},[a.Vector3TypeName]={compositionFieldNames=a.ListToMap({\'x\',\'y\',\'z\'}),compositionFieldLength=3,toTableFunc=a.Vector3ToTable,fromTableFunc=a.Vector3FromTable},[a.Vector4TypeName]={compositionFieldNames=a.ListToMap({\'x\',\'y\',\'z\',\'w\'}),compositionFieldLength=4,toTableFunc=a.Vector4ToTable,fromTableFunc=a.Vector4FromTable},[a.QuaternionTypeName]={compositionFieldNames=a.ListToMap({\'x\',\'y\',\'z\',\'w\'}),compositionFieldLength=4,toTableFunc=a.QuaternionToTable,fromTableFunc=a.QuaternionFromTable}}i=a.ListToMap({a.NegativeNumberTag,a.ArrayNumberTag})j=a.LogLevelInfo;l={[a.LogLevelFatal]=\'FATAL\',[a.LogLevelError]=\'ERROR\',[a.LogLevelWarn]=\'WARN\',[a.LogLevelInfo]=\'INFO\',[a.LogLevelDebug]=\'DEBUG\',[a.LogLevelTrace]=\'TRACE\'}package.loaded[\'cytanb\']=a;m,n=(function()local dd=\'eff3a188-bfc7-4b0e-93cb-90fd1adc508c\'local dj=_G[dd]if not dj then dj={}_G[dd]=dj end;local ff=dj.randomSeedValue;if not ff then ff=os.time()-os.clock()*10000;dj.randomSeedValue=ff;math.randomseed(ff)end;local fg=dj.clientID;if type(fg)~=\'string\'then fg=tostring(a.RandomUUID())dj.clientID=fg end;local fh=vci.state.Get(b)or\'\'if fh==\'\'and vci.assets.IsMine then fh=tostring(a.RandomUUID())vci.state.Set(b,fh)end;return fh,fg end)()return a end)()\n\nif vci.assets.IsMine then\n";

                s2 = string.Join("\n", emitCmnt);
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
                MessageBox.Show("プラグインを停止しました。\nThis plugin was stopped\n\n設定ファイルがありません。\nThere is no setting file.\n\n1. C:\\Users\\%ユーザー名%\\AppData\\Roaming\\posite-c\\NiconamaCommentViewer\\NtoV.txt を作成してください。\n   Please create the text file.\n\n2. NtoV.txt に CommentBaton VCI の場所 C:\\Users\\%ユーザー名%\\AppData\\LocalLow\\infiniteloopCo,Ltd\\VirtualCast\\EmbeddedScriptWorkspace\\CommentBaton を書いてください。\n   Please write the CommentBaton VCI directory in the text file.\n\n3. NCVを立ち上げなおしてください。\n   Please reboot NCV.", "NtoV エラー error");
            }
            else if (errorNumber == 2)
            {
                // プラグイン停止
                ONOFF = false;
                // タイマー停止
                timer.Stop();
                MessageBox.Show("プラグインを停止しました。\nThis plugin was stopped\n\n指定ディレクトリが CommentBaton ではありません。\nThe directory is not CommentBaton.\n\n1. NtoV.txt の内容（ CommentBaton VCI の場所 ）を確認してください。\n   Please check the CommentBaton directory in the NtoV.txt.\n\n2. NCVを立ち上げなおしてください。\n   Please reboot NCV.", "NtoV エラー error");
            }
            else if (errorNumber == 3)
            {
                // プラグイン停止
                ONOFF = false;
                // タイマー停止
                timer.Stop();
                MessageBox.Show("プラグインを停止しました。\nThis plugin was stopped\n\n指定ディレクトリがありません。\nThe directory does not Exist.\n\n1. NtoV.txt の内容（ CommentBaton VCI の場所 ）と実在を確認してください。\n   Please check the CommentBaton directory in the NtoV.txt and existence.\n\n2. NCVを立ち上げなおしてください。\n   Please reboot NCV.", "NtoV エラー error");
            }
        }

        /// <summary>
        /// 運営コメントを編集
        /// 参考 https://github.com/chinng-inta
        /// </summary>
        string editComment(string message)
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
