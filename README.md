# 📀 DVDPlayerBowlingSimulator 🎳

![dvdplayerbowlingsimulator_movie](/_img/dvdplayerbowlingsimulator_movie.gif)

**DVDプレイヤーボウリングシミュレータ for 魔改造の夜**

**DVD Player Bowling Simulator for MAKAIZOU NO YORU(NIGHT OF THE MAKAIZOU SOCIETY)**

## Introduction

[魔改造の夜 第4弾 「DVDプレーヤーボウリング」](https://www.nhk.jp/p/ts/6LQ2ZM4Z3Q/episode/te/BV4XV7KV9X/)でディスクとピンが当たったときにどうなるか「あたり」を付けるために物理エンジンを使用してシミュレータを作成しました。

ディスクがピンに接触する直前においてディスクの諸条件（位置、速度、回転、向きetc）を変えるとピンがどのように倒れるかのシミュレータになります。

.NETで作られた物理エンジン[bepuphysics v2](https://github.com/bepu/bepuphysics2)を使用しています。

## How to use

実行には[.NET5 Runtime](https://dotnet.microsoft.com/en-us/)が必要です。

本リポジトリをクローンしてビルドたまは、この[リリースファイル](https://github.com/tomitomi3/DVDPlayerBowlingSimulator/releases/download/V1.0.0_public_release/DVDPlayerBowlingSimulator.zip)をダウンロードして展開してください。フォルダ内の **「DVDPlayerBowlingSimulator.exe」** を実行してください。

キーボードで操作します。

「C」キーを押すとディスクが発射されます。

「V」キーを押すと視点を変えることが出来ます。

「F6」キーを押すと倒れたピンを削除します。

「F5」キーを押すとリセットします。

**ShootDiskSettings.json**ファイルでいろいろできます。

## Memo

* レギュレーション準拠
  * ピンの長さ、ディスクの厚み・大きさ
  * ピンの重さ、ディスクの重さ
    * ピン内部構造の不均一を円柱4つで近似。それぞれの重さを変更（＝重心がかわる）。
* ディスクの位置、向き、回転（角速度）、速度を変更可能
