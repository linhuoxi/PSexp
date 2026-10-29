#target photoshop
// 轻量身份探测，不枚举图层；同名文档也按 Photoshop 文档 ID 区分。
__psx_result = app.documents.length ? String(app.activeDocument.id) + '|' + app.activeDocument.name : 'none';
