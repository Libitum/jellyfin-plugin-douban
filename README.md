# Douban plugin for Jellyfin

## 1. Background

[Jellyfin](https://github.com/jellyfin/jellyfin) is a Free Software Media System
that puts you in control of managing and streaming your media. You can find
more information in the [homepage](https://jellyfin.media/)

[Douban](https://www.douban.com/) is a famous media information website in China.
It just like IMDB or TMDB.

This plugin is a remote metadata provider for Jellyfin, which can fetch metadata
for movies and TV series from Douban, including rating, summary, casts, etc.

## 2. Usage

1. Download from release page or compile by yourself.
1. Decompress the package, and put the "Douban" directory as the subdirectory of
"plugins" in Jellyfin.
    * For Linux, it's in "~/.local/jellyfin/config/plugins"
    * For Mac, it's in "~/.local/share/jellyfin/plugins"
    * For Docker, it's in "/config/plugins" inner Docker
    * For other system, if you cannot find it, please let me know.
1. Restart the Jellyfin service.

## 3. Features

1. Support most features of Movie and TV series.
1. Do not support merge different seasons into one.
1. Do not support fetching background image right now since Douban do not have
it. Will support it later.

## 4. Configuration

After installing the plugin, we can enable it in libraries for Movie and
TV series.

Firstly, we need to enable the advanced settings to enable image provider
which need to be configured later.
![enable advanced settings](assets/enable_advanced_settings.png?raw=true)

Secondly, please set the language to Chinese. Douban provider will not work in
other language.
![language and country](assets/language_and_country.png?raw=true)

Thirdly, please enable "Douban TV Provider" for your libraries. Besides, it's
"Douban Movie Provider" for type of Movie.
![enable douban provider](assets/enable_douban_provider.png?raw=true)

Finally, please enable "Douban Image Provider" as the Series image fetcher.
This is only available when you enable the advanced settings before. It could
has no posters without this setting.
![enable image provider](assets/enable_douban_image_provider.png?raw=true)

# 中文版

## 1. 背景

[Jellyfin](https://github.com/jellyfin/jellyfin) 是一个免费的多媒体数据管理软件。
详情请见[官网](https://jellyfin.media/)

[Douban](https://www.douban.com/)  豆瓣就不用介绍了吧:-)

这个插件是一个 Jellyfin 的元数据提取插件，能够从豆瓣抓取电影和电视剧的元数据，包括评分、简介、
演员等相关信息。

## 2. 使用方式

1. 从 Release 页面下载最新的版本，或者自行编译。
1. 把下载的文件解压，然后将 Douban 文件夹放到 Jellyfin 的 "plugins" 目录下。
    * 对于 Linux, plugins 目录在 "$HOME/.local/jellyfin/config/plugins"
    * 对于 Mac 系统, 在 "~/.local/share/jellyfin/plugins"
    * 对于 Docker, 在 Docker 中的 "/config/plugins" 目录下。 相应的宿主机目录请查阅自己
    的目录映射配置
    * 对于其他系统，如果你找不到位置，请提 issue 或者与我联系。
1. 重启 Jellyfin Service

## 3. 功能

1. 支持获取电影和电视剧类型的元数据
1. 不支持把多季的电视剧合并成一个。比如：权力的游戏每一季都是分开的，不支持合并在一起。
1. 不支持获取电影的背景图片，因为豆瓣没有大的海报图片。

## 4. 配置

TODO
