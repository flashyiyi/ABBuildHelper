# AB BuildHelper
菜单Window/AB BuildHelper下为功能入口
- AB Doctor负责检测资源臃余（同一资源打到多个包内）和内置资源的处理
- AB Viewer负责查看打好的包内资源，双击和拖动可进行测试加载
- AB PackRule自动设置AB Name
- AB FindUnpack检查遗漏未打包资源
- AB Build一个执行打包的界面


对官方工具的补充：
<https://github.com/Unity-Technologies/AssetBundles-Browser><br>
官方工具判重忽略了图集文件和BuildIn资源。
