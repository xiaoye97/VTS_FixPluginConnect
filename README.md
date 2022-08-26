# VTS_FixPluginConnect
修复VTube Studio无法连接任何插件的问题。
(VBridger连接不上手机不在此仓库的解决范围内)

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

## 问题出现的原因以及解决思路
由于`VTubeStudio`和VTubeStudio的插件API前置库`websocket-sharp`都使用了`Dns.GetHostAddresses`个`Dns.GetHostName()`方法来获取本机IP地址，并且尝试使用本机名称来作为参数。

但是此方法不支持双字节字符，中文就是典型的双字节字符，所以计算机名为中文的用户就无法正确获取本机IP。

那么想要解决这个问题就很简单，只要使用纯英文数字这种单字节的字符来作为名字就可以了。或者使用其他的方式来获取本机IP使用。此仓库内的插件实现了使用其他方式获取IP的功能，但是依旧建议使用改名方式来修复此问题，一劳永逸。