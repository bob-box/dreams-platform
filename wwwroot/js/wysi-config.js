const fullWysiTools = [
    'format', '|',
    'bold', 'italic', 'underline', 'strike', '|',
    'ul', 'ol', '|',
    'link', 'image', 'hr', 'quote', '|',
    'removeFormat'
];

const wysiToolProfiles = {
    full: fullWysiTools,
    noImages: fullWysiTools.filter(tool => tool !== 'image')
};

document.querySelectorAll('.wysi-editor').forEach(editor => {
    const profileName = editor.dataset.wysiProfile;
    const tools = wysiToolProfiles[profileName];

    if (!tools) {
        console.error(`Unknown Wysi tool profile "${profileName}" on`, editor);
        return;
    }

    if (!editor.id) {
        console.error('Wysi editor must have an id to be initialized.', editor);
        return;
    }

    Wysi({
        el: `#${CSS.escape(editor.id)}`,
        height: 300,
        autoGrow: true,
        autoHide: false,
        tools
    });
});
