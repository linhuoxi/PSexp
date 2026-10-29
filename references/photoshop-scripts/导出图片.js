$$var runCode1 = {自动重命名}; // true为不覆盖（自动重命名），false为覆盖

if (runCode1) {
  // 代码1
// 导出整个文档为指定格式的图片
function exportDocument(doc, outputFile, format) {
  var options;

  // 根据传入的格式名来选择导出选项
  if (format.toLowerCase() === "png") {
    options = new PNGSaveOptions();
  } else if (format.toLowerCase() === "jpg" || format.toLowerCase() === "jpeg") {
    options = new JPEGSaveOptions();
    options.quality = Math.round(12 * ({图片质量} / 100)); // 映射范围为0-12
  } else {
    alert("不支持的图片格式！");
    return;
  }

  // 导出图片
  doc.saveAs(outputFile, options, true);
}

// 获取当前文档
var currentDoc = app.activeDocument;

// 自定义导出路径和格式
var exportPath = "{导出路径}";
var format = "{图片格式}";
var customFileName = "{图片命名}";  // 这里设置自定义的文件名

// 判断输出路径是否存在同名文件
var suffix = "";
var fileIndex = 1;
var outputFile = new File(exportPath + customFileName + "." + format.toLowerCase());  // 使用自定义文件名

while(outputFile.exists) {
  suffix = "_" + fileIndex;
  outputFile = new File(exportPath + customFileName + suffix + "." + format.toLowerCase());
  fileIndex++;
}

// 将当前文档保存为指定格式
exportDocument(currentDoc, outputFile, format);

  // 继续添加代码1的其他部分
  
} else {
  // 代码2
// 导出整个文档为指定格式的图片
function exportDocument(doc, outputFile, format) {
  var options;

  // 根据传入的格式名来选择导出选项
  if (format.toLowerCase() === "png") {
    options = new PNGSaveOptions();
  } else if (format.toLowerCase() === "jpg" || format.toLowerCase() === "jpeg") {
    options = new JPEGSaveOptions();
      options.quality = Math.round(12 * ({图片质量} / 100)); // 映射范围为0-12
  } else {
    alert("不支持的图片格式！");
    return;
  }

  // 导出图片
  doc.saveAs(outputFile, options, true);
}

// 获取当前文档
var currentDoc = app.activeDocument;

// 自定义导出路径和格式
var exportPath = "{导出路径}";
var format = "{图片格式}";
var customFileName = "{图片命名}";  // 这里设置自定义的文件名

// 将当前文档保存为指定格式
var outputFile = new File(exportPath + customFileName + "." + format.toLowerCase());  // 使用自定义文件名
exportDocument(currentDoc, outputFile, format);


  // 继续添加代码2的其他部分
}
