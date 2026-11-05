# SongCore JSON Cache Fix

## 概要

Beat Saber 1.44.1 の SongCore 3.16.0 では、JSON の読み込みタスクを、要求された譜面と一致するかを確認せずに再利用することがあります。この Plugin はその対策です。

選んだ難易度と出てくるノーツが食い違う原因が、この JSON キャッシュにあるときにだけ入れてください。そうでない場合は不要です。

キャッシュされた JSON と進行中の読み込みは、`BeatmapKey` が一致するときだけ再利用します。

導入できているとき、Multiplayer+ の部屋タイトルの上に `SongCore JSON Cache Fixed!!` と表示します。Mod 設定の **SongCore JSON Cache Fix** にある **Show status text** で、この表示を切り替えられます。

### 対応環境

- Beat Saber 1.44.1
- SongCore 3.16.0
- BSIPA 4.3 以降
- BSML 1.12 以降

### 導入

[ここ](https://github.com/SatoDoge/SongCoreJsonCacheFix/releases/latest)から最新ビルドをダウンロード・解凍し`SongCoreJsonCacheFix.dll` を Beat Saber の `Plugins` フォルダに置きます。

Pluginを外したい場合はSongCoreJsonCacheFix.dllを削除してください。

### ビルド

```
dotnet build -c Release -p:BeatSaberDir="C:\path\to\Beat Saber"
```

DLL は `bin/Release/net472/SongCoreJsonCacheFix.dll` にできます。
