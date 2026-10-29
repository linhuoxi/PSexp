var docName = app.activeDocument.name; // 获取文档名称

var fileExtension = docName.split('.').pop(); // 获取文件后缀

var fileNameWithoutExtension = docName.replace('.' + fileExtension, ''); // 去除文件后缀

var v = fileNameWithoutExtension; // 将去除后缀的文档名称赋值给变量 v

// 输出变量 v
v;
