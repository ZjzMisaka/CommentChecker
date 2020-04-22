# CommentChecker
 修改代码周围注释配对检查程序
## 用途 
- 适用于需要修改很多代码, 并且修改后的代码周围需要添加注释, 并且自己非常手残, 经常写错 / 写漏注释的情况. 
### 举例
``` csharp
// #XXXX XXX 20200422 START
// 修改的代码
// #XXXX XXX 20200422 END
```
写成: 
``` csharp
// #XXXX XXX 20200422 START
// 修改的代码
// #XXXX XXX 20200422 EMD
```
或者: 
``` csharp
// #XXXX XXX 20200422 START
// #XXXX XXX 20200422 START
// 修改的代码
// #XXXX XXX 20200422 END
```
等等情况. 
