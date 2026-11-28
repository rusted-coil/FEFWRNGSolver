export async function copyCsv(source, unavailable, denied) {
    const readCsv = () => source.invokeMethod("CreatePredictionCsv");
    try {
        if (window.ClipboardItem && navigator.clipboard?.write) {
            // Request clipboard access during the click, before formatting a large result set.
            const content = Promise.resolve().then(() => new Blob([readCsv()], { type: "text/plain" }));
            await navigator.clipboard.write([new ClipboardItem({ "text/plain": content })]);
        } else if (navigator.clipboard?.writeText) {
            await navigator.clipboard.writeText(readCsv());
        } else {
            return unavailable;
        }
        return null;
    } catch (error) {
        const reason = String(error?.message || error?.name || error).split(/[\r\n]/, 1)[0];
        return error?.name === "NotAllowedError" ? denied + " " + reason : reason;
    }
}

export async function saveCsv(source, suggestedName, description, filenamePrompt) {
    const makeFile = () => new Blob(["\uFEFF", source.invokeMethod("CreatePredictionCsv")], { type: "text/csv;charset=utf-8" });
    if (typeof window.showSaveFilePicker === "function") {
        // Start the picker during the click, then capture the results before any later edits.
        const selection = window.showSaveFilePicker({
            suggestedName, types: [{ description, accept: { "text/csv": [".csv"] } }]
        }).catch(error => {
            if (error.name === "AbortError") return null;
            throw error;
        });
        const [handle, file] = await Promise.all([selection, Promise.resolve().then(makeFile)]);
        if (handle)
            await file.stream().pipeTo(await handle.createWritable());
        return;
    }

    const name = window.prompt(filenamePrompt, suggestedName)?.trim();
    if (!name) return;
    const link = document.createElement("a");
    const url = URL.createObjectURL(makeFile());
    link.href = url;
    link.download = /\.csv$/i.test(name) ? name : name + ".csv";
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 60000);
}
