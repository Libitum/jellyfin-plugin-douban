# Douban plugin for Jellyfin
## 1. Background
[Jellyfin](https://github.com/jellyfin/jellyfin) is a Free Software Media System that puts you in control of managing and streaming your media. You can find more information in the [homepage](https://jellyfin.media/)

[Douban](https://www.douban.com/) is a famous media information website in China. It just like IMDB or TMDB.

This plugin is a remote metadata provider for Jellyfin, which can fetch metadata for movies and TV series from Douban, including rating, summary, casts, etc.

## 2. Usage
1. Download from release page or compile by yourself.
1. Decompress the package, and put the douban directory as the subdirectory of "plugins" in Jellyfin.
    * For Linux, it's in "~/.local/jellyfin/config/plugin"
    * For other system, if you cannot find it, please let me know.
1. Restart the Jellyfin service.

## 3. Features
1. Support movie media type
1. Do not support fetching background image right now since Douban do not have it.
1. Do not support TV right now since Douban has different meta structure from TMDB. Will support later.

## 4. Configuration
TODO

# 中文版
## 1. 背景
[Jellyfin](https://github.com/jellyfin/jellyfin) 是一个免费的多媒体数据管理软件。详情请见[官网](https://jellyfin.media/)

[Douban](https://www.douban.com/)  豆瓣就不用介绍了吧:-)

这个插件是一个 Jellyfin 的元数据提取插件，能够从豆瓣抓取电影和电视剧的元数据，包括评分、简介、演员等相关信息。

## 2. 使用方式
1. 从 Release 页面下载最新的版本，或者自行编译。
1. 把下载的文件解压，然后将 douban 文件夹放到 Jellyfin 的 "plugins" 目录下。
    * 对于 Linux, plugins 目录在 "$HOME/.local/jellyfin/config/plugin"
    * 对于其他系统，如果你找不到位置，请提 issue 或者与我联系。
1. 重启 Jellyfin Service

## 3. 功能
1. 支持获取电影类型的元数据
2. 不支持获取电影的背景图片，因为豆瓣没有大的海报图片。
3. 不支持电视剧类型，因为豆瓣的数据组织形式和老美的不太一样，所以还要在看一下怎么实现。

## 4. 配置
TODO