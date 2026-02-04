

クライアントサイド

フォルダを同期
バージョンのロールバックもしたいので、ファイル日時が異なったらダウンロードする

同期後、exeを起動
SyncBridge自体は ClickOnce / exe 起動で呼び出せる
受けた引数をそのまま渡して他のexeを起動
拡張子およびプロトコルからの起動も受け付けるようにする？
引数によってSyncBridgeが起動するアプリの分岐などもあり？

SyncBridge自体は.NET Framework 4.6.1 Forms とする

Consoleアプリによるブートストラッパーを介してアプリを起動します。
 ConsoleアプリはClickOnce配布とします。 
 アプリ起動前に以下のフォルダを同期します。 
同期場所は設定で指定できるものとします。（バージョンは一例)

(%LocalAppData%\Company\SyncBridge\ 内を想定)
runtime/dotnet-10.0.2/(.NET Desktop Runtime を含む dotnet ルート一式)
apps/AppA-1.4.7/
apps/AppB-2.1.0/
manifest.json(起動バージョン指定など)

アプリAとアプリBはともにランタイム同梱なしで、ランタイムはdotnetフォルダにあるものを使用します。
Consoleアプリで念のため ProcessStartInfo.EnvironmentVariables(DOTNET_ROOT_X64) を変更して、UseShellExecute = false で、dotnetフォルダを指定します。
Consoleアプリで (target)/runtimne/dotnet-x.x.x/dotnet.exe (target)/apps/AppA-x.x.x/AppA.dll とフルパスで起動します。
ただし、dotnet.exe指定の場合はDOTNET_ROOT_X64は必須ではありません。



