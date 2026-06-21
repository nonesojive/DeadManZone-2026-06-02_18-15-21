#target photoshop
// ponytail: one-shot batch split for two 3x3 Grok sheets; re-run if sheet layout changes.
app.displayDialogs = DialogModes.NO;

var OUT_DIR = new Folder("C:/Users/jiveg/OneDrive/Desktop/Game Projects/DeadManZone/Assets/grok tiles");
var SOURCES = [
    {
        file: "C:/Users/jiveg/AppData/Roaming/Cursor/User/workspaceStorage/empty-window/images/grok-image-1e7a502f-52cc-4c09-8141-4c148cebd946-dc470f91-b1c0-4eb4-9d2c-2c26255d569d.png",
        startTile: 1
    },
    {
        file: "C:/Users/jiveg/AppData/Roaming/Cursor/User/workspaceStorage/empty-window/images/grok-image-566e9e81-26bd-4121-adaf-3e2bf0f7a78d-ca579146-cd45-4e82-8487-88db70efe813.png",
        startTile: 10
    }
];

// Detected gutters from sheet1; sheet2 shares the same Grok grid layout.
var RECTS = [
    [22, 20, 330, 330],
    [357, 20, 665, 330],
    [691, 20, 1000, 330],
    [22, 356, 330, 666],
    [357, 356, 665, 666],
    [691, 356, 1000, 666],
    [22, 692, 330, 1001],
    [357, 692, 665, 1001],
    [691, 692, 1000, 1001]
];

// Background removal: Photoshop color-range delete is inconsistent across versions.
// After running this script, run remove_grok_tile_background.ps1 (edge flood + inner black).

function savePng(doc, file) {
    var opts = new PNGSaveOptions();
    opts.compression = 6;
    opts.interlaced = false;
    doc.saveAs(file, opts, true, Extension.LOWERCASE);
}

function unlockLayer(doc) {
    if (doc.activeLayer.isBackgroundLayer) {
        doc.activeLayer.isBackgroundLayer = false;
    }
}

function selectDarkBackground(doc) {
    var desc = new ActionDescriptor();
    var ref = new ActionReference();
    ref.putProperty(charIDToTypeID("Chnl"), charIDToTypeID("fsel"));
    desc.putReference(charIDToTypeID("null"), ref);

    var range = new ActionDescriptor();
    range.putInteger(charIDToTypeID("Fzns"), 32);
    range.putInteger(charIDToTypeID("Mnm "), 0);
    range.putInteger(charIDToTypeID("Mxm "), DARK_MAX);

    var minRGB = new ActionDescriptor();
    minRGB.putDouble(charIDToTypeID("Rd  "), 0);
    minRGB.putDouble(charIDToTypeID("Grn "), 0);
    minRGB.putDouble(charIDToTypeID("Bl  "), 0);
    range.putObject(charIDToTypeID("Mnm "), charIDToTypeID("RGBC"), minRGB);

    var maxRGB = new ActionDescriptor();
    maxRGB.putDouble(charIDToTypeID("Rd  "), DARK_MAX);
    maxRGB.putDouble(charIDToTypeID("Grn "), DARK_MAX);
    maxRGB.putDouble(charIDToTypeID("Bl  "), DARK_MAX);
    range.putObject(charIDToTypeID("Mxm "), charIDToTypeID("RGBC"), maxRGB);

    desc.putObject(charIDToTypeID("T   "), charIDToTypeID("Clrs"), range);
    executeAction(charIDToTypeID("setd"), desc, DialogModes.NO);
}

function removeDarkBackground(doc) {
    unlockLayer(doc);
    selectDarkBackground(doc);
    if (doc.selection.bounds) {
        doc.selection.clear();
    }
    doc.selection.deselect();
}

function cropRect(doc, rect) {
    var bounds = [
        new UnitValue(rect[0], "px"),
        new UnitValue(rect[1], "px"),
        new UnitValue(rect[2] + 1, "px"),
        new UnitValue(rect[3] + 1, "px")
    ];
    doc.crop(bounds);
}

function processSheet(sourcePath, startTile) {
    var srcFile = new File(sourcePath);
    if (!srcFile.exists) {
        throw new Error("Missing source: " + sourcePath);
    }

    var sheet = app.open(srcFile);
    var exported = [];

    for (var i = 0; i < RECTS.length; i++) {
        var tileNum = startTile + i;
        var tileDoc = sheet.duplicate("tile" + tileNum, true);
        cropRect(tileDoc, RECTS[i]);
        removeDarkBackground(tileDoc);

        var outFile = new File(OUT_DIR.fsName + "/tile" + tileNum + ".png");
        savePng(tileDoc, outFile);
        exported.push(outFile.fsName);
        tileDoc.close(SaveOptions.DONOTSAVECHANGES);
    }

    sheet.close(SaveOptions.DONOTSAVECHANGES);
    return exported;
}

if (!OUT_DIR.exists) {
    OUT_DIR.create();
}

var allExported = [];
for (var s = 0; s < SOURCES.length; s++) {
    allExported = allExported.concat(processSheet(SOURCES[s].file, SOURCES[s].startTile));
}

return {
    ok: true,
    count: allExported.length,
    files: allExported
};
