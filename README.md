# Douban plugin for Jellyfin

## 0. 写在前面的话

这个项目是我在 19 年折腾 NAS 的时候心血来潮写的小玩具，当时还不会 c#，由于工作很忙，只能一点点边学边折腾。

时过境迁，没想到这么多年过去了，反而是工作清闲下来了（心酸）。朝花夕拾，权当纪念~

## 1. 背景

[Jellyfin](https://github.com/jellyfin/jellyfin) 是一个免费的多媒体数据管理软件。
详情请见[官网](https://jellyfin.media/)

[Douban](https://www.douban.com/)  豆瓣就不用介绍了吧:-)

这个插件是一个 Jellyfin 的元数据提取插件，能够从豆瓣抓取电影和电视剧的元数据，包括评分、简介、
演员等相关信息。

## 2. 使用方式

### 通过插件仓库安装

1. 插件仓库地址：https://github.com/Libitum/jellyfin-plugin-douban/releases/latest/download/manifest.json

TODO：截图介绍过程

### 手动安装

1. 从 Release 页面下载最新的版本，或者自行编译。
2. 把下载的文件解压，然后将 Douban 文件夹放到 Jellyfin 的 "plugins" 目录下。
   * 对于 Linux, plugins 目录在 "$HOME/.local/share/jellyfin/plugins"
   * 对于 Mac 系统, 在 "~/.local/share/jellyfin/plugins"
   * 对于 Docker, 在 Docker 中的 "/config/plugins" 目录下。 相应的宿主机目录请查阅自己
     的目录映射配置
   * 对于 Windows 10, 如果使用管理员权限启动的话，在 "C:\ProgramData\Jellyfin\Server\plugins" 目录下。
   * 对于其他系统，如果你找不到位置，请提 issue 或者与我联系。
3. 重启 Jellyfin Service

## 3. 功能

1. 支持获取电影和电视剧类型的元数据；
2. 支持获取部分电影的背景图片，会在豆瓣海报中尝试寻找合适的图片；
3. 支持延迟请求，避免被豆瓣官方封禁；
4. 不支持把多季的电视剧合并成一个。比如：权力的游戏每一季都是分开的，不支持合并在一起。

## 4. 配置

TODO

