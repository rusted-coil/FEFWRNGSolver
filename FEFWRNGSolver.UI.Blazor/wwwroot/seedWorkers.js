export function createPool() {
    const slots = [];
    let failure;

    function stop(reason) {
        if (failure) return;
        if (reason) console.warn("成長seed探索ワーカーを停止します。", reason);
        failure = new Error(reason ?? "探索を終了しました。");
        for (const slot of slots) {
            slot.worker.terminate();
            if (slot.pending) {
                clearTimeout(slot.pending.timer);
                slot.pending.reject(failure);
                slot.pending = null;
            }
        }
    }

    function request(slot, message) {
        if (failure) return Promise.reject(failure);
        return new Promise((resolve, reject) => {
            const timer = setTimeout(() => stop("探索ワーカーから応答がありません。"), 60000);
            slot.pending = { resolve, reject, timer };
            try {
                slot.worker.postMessage(message);
            } catch (error) {
                stop(error.message);
            }
        });
    }

    return {
        async start(rates, observations) {
            // Worker 内にはページの import map が適用されないため、ここで公開用 URL を解決する。
            const runtimeUrl = import.meta.resolve(new URL("_framework/dotnet.js", document.baseURI).href);
            const workerUrl = import.meta.resolve("./seedWorker.js");
            const cores = navigator.hardwareConcurrency || 2;
            const memory = navigator.deviceMemory;
            const limit = memory <= 2 ? 1 : memory <= 4 ? 2 : 4;
            const count = Math.min(limit, cores > 2 ? cores - 1 : cores);
            for (let i = 0; i < count; i++) {
                const worker = new Worker(workerUrl, { type: "module", name: `seed-search-${i + 1}` });
                const slot = { worker, pending: null };
                slots.push(slot);
                worker.onerror = event => stop(event.message || "探索ワーカーが停止しました。");
                worker.onmessageerror = () => stop("探索ワーカーの応答を読み取れませんでした。");
                worker.onmessage = ({ data }) => {
                    if (data.error) {
                        stop(data.error);
                    } else if (slot.pending) {
                        clearTimeout(slot.pending.timer);
                        slot.pending.resolve(data.result);
                        slot.pending = null;
                    }
                };
            }
            const settings = { runtimeUrl, rates, observations: observations.flat() };
            await Promise.all(slots.map(slot => request(slot, settings)));
            return count;
        },
        scan(index, start, end, sampleLimit) {
            return request(slots[index], { start, end, sampleLimit });
        },
        stop
    };
}
