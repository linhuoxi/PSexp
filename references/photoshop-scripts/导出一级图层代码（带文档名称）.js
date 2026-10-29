$$// 参数配置
var runCode1 = {自动重命名}; // true为不覆盖（自动重命名），false为覆盖
var exportFormat = "{图片格式}"; // 例如 "jpg"
var qualitySetting = {图片质量}; // 例如 60
var exportPath = "{导出路径}\\";

// 显示所有非排除图层（用于导出完成后还原状态）
function showAllLayersExceptBackground(doc) {
  for (var i = 0; i < doc.layers.length; i++) {
    var currentLayer = doc.layers[i];
    if (!currentLayer.isBackgroundLayer && !/^排除/i.test(currentLayer.name)) {
      currentLayer.visible = true;
    }
  }
}

// 导出所有图层并附带背景图层
function exportAllLayersWithBackground(format, renameIfExists) {
  var currentDoc = app.activeDocument;

  // 获取文档名（不含扩展名），用于命名前缀
  var docName = currentDoc.name.replace(/\.[^\.]+$/, "").replace(/[\/:*?"<>|]/g, "_");

  // 检查背景图层
  var hasBackgroundLayer = currentDoc.artLayers && currentDoc.artLayers.length > 0 && currentDoc.artLayers[currentDoc.artLayers.length - 1].isBackgroundLayer;
  var backgroundLayer = hasBackgroundLayer ? currentDoc.artLayers[currentDoc.artLayers.length - 1] : null;

  // 创建进度条窗口
  var progressBarWindow = new Window("palette", "导出中,勿乱动,易卡顿....");
  var progressBarLabel = progressBarWindow.add("statictext", undefined, "正在导出: ");
  progressBarLabel.preferredSize.width = 500;
  var progressBar = progressBarWindow.add("progressbar", undefined, 0, currentDoc.layers.length);
  progressBar.preferredSize.width = 300;
  var timeRemainingLabel = progressBarWindow.add("statictext", undefined, "剩余时间: 计算中...");
  var cancelButton = progressBarWindow.add("button", undefined, "取消");

  var isCancelled = false;
  cancelButton.onClick = function () {
    stopExport();
  };

  progressBarWindow.show();

  var startTime = new Date();
  recordLayerVisibility(currentDoc);

  for (var i = 0; i < currentDoc.layers.length; i++) {
    var currentLayer = currentDoc.layers[i];

    if (currentLayer.isBackgroundLayer || /^排除/i.test(currentLayer.name)) {
      continue;
    }

    hideAllLayersExceptOne(currentLayer, backgroundLayer);

    var layerName = currentLayer.name.replace(/[\/:*?"<>|]/g, "_");
    var fileName = docName + "_" + layerName;
    var outputFile = new File(exportPath + fileName + "." + format.toLowerCase());

    if (renameIfExists) {
      var fileIndex = 1;
      while (outputFile.exists) {
        outputFile = new File(exportPath + fileName + "_" + (fileIndex++) + "." + format.toLowerCase());
      }
    }

    exportDocument(currentDoc, outputFile, format);

    // 更新进度条
    progressBar.value = i + 1;
    progressBarLabel.text = "正在导出: " + truncateLayerName(currentLayer.name, 40);
    var elapsedTime = (new Date() - startTime) / 1000;
    var layersProcessed = i + 1;
    var layersRemaining = currentDoc.layers.length - layersProcessed;
    var estimatedTotalTime = (elapsedTime / layersProcessed) * currentDoc.layers.length;
    var timeRemaining = estimatedTotalTime - elapsedTime;
    timeRemainingLabel.text = "剩余时间: " + Math.round(timeRemaining) + " 秒";

    progressBarWindow.update();
    $.sleep(0);

    if (isCancelled) {
      break;
    }

    showAllLayersExceptBackground(currentDoc);
  }

  restoreLayerVisibility(currentDoc);
  progressBarWindow.close();

  if (isCancelled && outputFile.exists) {
    outputFile.remove();
  }
}

// 停止导出
function stopExport() {
  isCancelled = true;
}

// 导出文档为指定格式
function exportDocument(doc, outputFile, format) {
  var options;
  if (format.toLowerCase() === "png") {
    options = new PNGSaveOptions();
  } else if (format.toLowerCase() === "jpg" || format.toLowerCase() === "jpeg") {
    options = new JPEGSaveOptions();
    options.quality = Math.round((qualitySetting / 100) * 12);
  } else {
    alert("不支持的图片格式！");
    return;
  }
  doc.saveAs(outputFile, options, true);
}

// 隐藏除当前图层与背景外的所有图层
function hideAllLayersExceptOne(selectedLayer, backgroundLayer) {
  var layers = selectedLayer.parent.layers;
  for (var i = 0; i < layers.length; i++) {
    var currentLayer = layers[i];
    if (currentLayer != selectedLayer && currentLayer != backgroundLayer && !/^排除/i.test(currentLayer.name)) {
      currentLayer.visible = false;
    }
  }
}

// 记录图层可见性状态
var layerVisibilityMap = {};
function recordLayerVisibility(doc) {
  for (var i = 0; i < doc.layers.length; i++) {
    var currentLayer = doc.layers[i];
    layerVisibilityMap[currentLayer.id] = currentLayer.visible;
  }
}

// 恢复图层可见性状态
function restoreLayerVisibility(doc) {
  for (var i = 0; i < doc.layers.length; i++) {
    var currentLayer = doc.layers[i];
    if (layerVisibilityMap.hasOwnProperty(currentLayer.id)) {
      currentLayer.visible = layerVisibilityMap[currentLayer.id];
    }
  }
}

// 截断图层名，避免过长
function truncateLayerName(name, maxLength) {
  if (name.length > maxLength) {
    return name.substring(0, maxLength - 3) + "...";
  }
  return name;
}

// 运行导出
recordLayerVisibility(app.activeDocument);
if (runCode1) {
  exportAllLayersWithBackground(exportFormat, true);
} else {
  exportAllLayersWithBackground(exportFormat, false);
}
restoreLayerVisibility(app.activeDocument);
