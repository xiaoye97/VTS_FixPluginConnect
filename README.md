# VTS_FixPluginConnect
修复VTube Studio无法连接任何插件的问题

视频教程请看B站 宵夜97

## 问题描述
VTube Studio的所有插件都无法连接

## 问题解决方案一(推荐)
1. 修改计算机名为纯英文
2. 修改电脑用户名为纯英文
3. 确保VTS和其他插件的安装路径为纯英文

## 问题解决方案二(备用，适用于VBridger，其他插件未测试)
1. 下载Releases中的最新版本
2. 将BepInEx解压到VTS和VBridger的根目录(如果VTS安装过XYPlugin，则只解压到VBridger即可)
3. 将VTS_FixPluginConnect.dll放到VTube Studio\BepInEx\plugins\VTS_Ex文件夹(没有则新建)
4. 将VB_FixPluginConnect.dll放到VBridger\BepInEx\plugins文件夹(没有则新建)