$$var runCode1 = {自动重命名}; // 假设为true，如果需要改为false，请自行修改，false是覆盖，true是不覆盖

if (runCode1) {
  // 代码1
  var doc = app.activeDocument;
  var exportOptions = new ExportOptionsSaveForWeb();
  //var doc = app.activeDocument;
var exportOptions = new ExportOptionsSaveForWeb();
exportOptions.format = SaveDocumentType.JPEG;
exportOptions.quality = {图片质量};

var slices = doc.layers;
var sliceIndex = 0;

var sliceFileName = "{图片命名}";
var exportPath = "{导出路径}";
var folderIndex = 1;  // 将初始文件夹索引设置为1，以便生成"图片_1"的文件夹
var folderPath = exportPath + sliceFileName + "_" + folderIndex + "\\";
var folder = new Folder(folderPath);

while (folder.exists) {
   // 如果文件夹已存在，则增加文件夹索引
   folderIndex++;
   folderPath = exportPath + sliceFileName + "_" + folderIndex + "\\";
   folder = new Folder(folderPath);
}

folder.create(); // 创建新的文件夹

for (var i = 0; i < slices.length; i++) {
  var slice = slices[i];

  if (slice.visible) {
    doc.activeLayer = slice;

    var sliceName = sliceFileName + "_" + sliceIndex;
    var filePath = folderPath + sliceName + ".jpg";

    // 处理同名文件
    var num = 1;
    while (File(filePath).exists) {
      sliceName = sliceFileName + "_" + sliceIndex + "_" + ("0" + num).slice(-2);
      filePath = folderPath + sliceName + ".jpg";
      num++;
    }

    doc.exportDocument(new File(filePath), ExportType.SAVEFORWEB, exportOptions);

    sliceIndex++;
    $.writeln("导出切片 " + sliceIndex + "：" + sliceName);

    // 只导出第一个切片，然后跳出循环
    break;
  }
}

$.writeln("导出完成！共导出 " + sliceIndex + " 个切片。");

  
  // 继续添加代码1的其他部分
  
} else {
  // 代码2
  var doc = app.activeDocument;
  var exportOptions = new ExportOptionsSaveForWeb();
  //var doc = app.activeDocument;
var exportOptions = new ExportOptionsSaveForWeb();
exportOptions.format = SaveDocumentType.JPEG;
exportOptions.quality = {图片质量};

// 获取所有切片
var slices = doc.layers;
var sliceIndex = 0;

// 设置导出的文件名
var sliceFileName = "{图片命名}";

// 遍历每个切片并导出为JPG
for (var i = 0; i < slices.length; i++) {
  var slice = slices[i];

  // 判断切片是否可见
  if (slice.visible) {
    doc.activeLayer = slice;

    // 使用自定义的文件名生成文件路径
    var sliceName = sliceFileName + "_"
    var exportPath = "{导出路径}" + sliceName + ".jpg";

    // 导出为JPG
    doc.exportDocument(new File(exportPath), ExportType.SAVEFORWEB, exportOptions);

    // 输出导出信息
    sliceIndex++;
    $.writeln("导出切片 " + sliceIndex + "：" + sliceName);
  
    // 只导出第一个切片，然后跳出循环
    break;
  }
}

// 完成导出
$.writeln("导出完成！共导出 " + sliceIndex + " 个切片。");

  
  // 继续添加代码2的其他部分
}
