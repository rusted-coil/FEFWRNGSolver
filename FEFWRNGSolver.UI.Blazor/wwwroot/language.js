const storageKey = "FEFWRNGSolver.language";

export function preferredLanguages() {
    return navigator.languages.length ? [...navigator.languages] : [navigator.language];
}

export function savedLanguage() {
    try {
        return localStorage.getItem(storageKey);
    } catch {
        return null;
    }
}

export function setDocumentLanguage(languageTag) {
    document.documentElement.lang = languageTag;
}

export function saveLanguage(code) {
    try {
        localStorage.setItem(storageKey, code);
    } catch {
        // ストレージが無効でも、このページを開いている間は選択を維持する。
    }
}
