export function init(dotNetRef) {
    const handleKeyDown = (e) => {
        // Ctrl + K or Cmd + K
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('HandleGlobalHotkey');
        }
    };

    window.addEventListener('keydown', handleKeyDown);

    return {
        dispose: () => {
            window.removeEventListener('keydown', handleKeyDown);
        }
    };
}
