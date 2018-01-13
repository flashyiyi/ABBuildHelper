# AB BuildHelper
菜单Window/AB BuildHelper下为功能入口
- AB Doctor负责检测资源臃余（同一资源打到多个包内）和内置资源的处理
- AB Viewer负责查看打好的包内资源，双击和拖动可进行测试加载

##预计功能：
- 自动设置AB Name并打包
- 检查遗漏未打包资源
- 界面优化


对官方工具的补充：
<https://github.com/Unity-Technologies/AssetBundles-Browser><br>
所以重复的功能并没有认真做，手动分包还是用官方这个。但官方工具判重忽略了图集文件和BuildIn资源。
