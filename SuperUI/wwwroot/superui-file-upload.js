// SgFileUpload: enables drag-and-drop file ingestion.
// Blazor's InputFile wraps a native <input type="file"> but cannot be fed
// directly from a DataTransfer. We grab the raw drop event here, write the
// dropped files into the underlying input and re-dispatch 'change' so the
// component's existing OnFileInputChange path (validation, previews, binding)
// runs unchanged.
function ingest(wrapper, dataTransfer) {
    var input = wrapper && wrapper.querySelector('input[type="file"]');
    if (!input || !dataTransfer || !dataTransfer.files || dataTransfer.files.length === 0) {
        return;
    }
    try {
        input.files = dataTransfer.files;
    }
    catch (e) {
        // Setting files via assignment is supported on modern browsers.
        // Fall back to a synthetic DataTransfer populated from the first file.
        if (typeof DataTransfer === 'function') {
            var dt = new DataTransfer();
            for (var i = 0; i < dataTransfer.files.length; i++) {
                dt.items.add(dataTransfer.files[i]);
            }
            input.files = dt.files;
        }
        else {
            return;
        }
    }
    input.dispatchEvent(new Event('change', { bubbles: true }));
}

export function initDropZone(wrapper) {
    if (!wrapper || wrapper.__superuiFileUploadBound) {
        return;
    }
    wrapper.__superuiFileUploadBound = true;
    wrapper.addEventListener('drop', function (e) {
        e.preventDefault();
        ingest(wrapper, e.dataTransfer);
    }, false);
}