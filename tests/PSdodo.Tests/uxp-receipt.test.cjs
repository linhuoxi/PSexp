const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.join(__dirname, '../../PSdodo/Resources/Scripts/export');
const wrapper = fs.readFileSync(path.join(root, 'receipt_task.psjs'), 'utf8');
(async () => {
    let count = 0;
    for (const mode of ['quick_export', 'all_artboards', 'selected_artboards']) {
        for (const scenario of ['success', 'no document', 'action error', 'throw', 'no output']) {
            const reports = [];
            const args = { EXPORT_PATH: 'C:/output/', FORMAT: 'png', QUALITY: '32', RENAME: 'true' };
            const body = fs.readFileSync(path.join(root, mode + '.psjs'), 'utf8').replace(/\{\{([A-Z_]+)\}\}/g, (_, key) => args[key]);
            const script = wrapper.replace('{{SCRIPT_BODY}}', body).replace('{{RESULT_FOLDER_URL}}', 'file:/task').replace('{{EXPORT_FOLDER_URL}}', 'file:/output');
            const ps = {
                app: { documents: scenario === 'no document' ? [] : [{}] },
                core: { async executeAsModal(fn) { await fn(); }, showAlert() { throw new Error('Unexpected Photoshop dialog'); } },
                action: { async batchPlay() {
                    if (scenario === 'throw') throw new Error('action threw');
                    return scenario === 'action error' ? [{ _obj: 'error', message: 'action failed' }] : [{}];
                } }
            };
            const uxp = { storage: { localFileSystem: { async getEntryWithUrl(url) {
                return url === 'file:/task' ? { async createFile() { return { async write(json) { reports.push(JSON.parse(json)); } }; } }
                    : { async getEntries() { return scenario === 'no output' ? [] : [{ name: 'output.png', isFile: true }]; } };
            } } } };
            await new vm.Script('(async()=>{' + script + '})()').runInNewContext({ require(name) { return name === 'photoshop' ? ps : uxp; } });
            const result = reports.at(-1);
            assert.equal(result.done, true);
            assert.equal(result.success, scenario === 'success');
            if (scenario !== 'success') assert.ok(result.error.length > 0);
            console.log('PASS ' + mode + ': ' + scenario); count++;
        }
    }
    console.log(count + ' UXP completion behavior tests passed.');
})().catch(error => { console.error(error); process.exitCode = 1; });
