mergeInto(LibraryManager.library, {
    UEmueraPickGameFiles: function (receiverPtr, persistentRootPtr) {
        var receiver = UTF8ToString(receiverPtr);
        var persistentRoot = UTF8ToString(persistentRootPtr).replace(/\\/g, '/').replace(/\/$/, '');
        var input = document.createElement('input');
        input.type = 'file';
        input.multiple = true;
        input.setAttribute('webkitdirectory', '');
        input.setAttribute('directory', '');
        input.style.display = 'none';
        document.body.appendChild(input);

        function notify(status) {
            if (typeof SendMessage === 'function')
                SendMessage(receiver, 'OnWebGLFolderImportFinished', status);
            if (input.parentNode)
                input.parentNode.removeChild(input);
        }

        function safeRelativePath(value) {
            var parts = String(value || '').replace(/\\/g, '/').split('/');
            var safe = [];
            for (var i = 0; i < parts.length; i++) {
                var part = parts[i];
                if (!part || part === '.')
                    continue;
                if (part === '..' || part.indexOf('\0') !== -1)
                    return null;
                safe.push(part);
            }
            return safe.length ? safe.join('/') : null;
        }

        input.addEventListener('change', function () {
            var files = Array.prototype.slice.call(input.files || []);
            if (!files.length) {
                notify('cancelled');
                return;
            }

            var writes = files.map(function (file) {
                return new Promise(function (resolve, reject) {
                    var relative = safeRelativePath(file.webkitRelativePath || file.name);
                    if (!relative) {
                        reject(new Error('invalid relative file path'));
                        return;
                    }
                    var target = persistentRoot + '/' + relative;
                    var separator = target.lastIndexOf('/');
                    var directory = separator > 0 ? target.substring(0, separator) : persistentRoot;
                    try {
                        FS.mkdirTree(directory);
                    } catch (e) {
                        reject(e);
                        return;
                    }

                    var reader = new FileReader();
                    reader.onerror = function () { reject(new Error('failed to read browser file')); };
                    reader.onload = function () {
                        try {
                            FS.writeFile(target, new Uint8Array(reader.result));
                            resolve();
                        } catch (e) {
                            reject(e);
                        }
                    };
                    reader.readAsArrayBuffer(file);
                });
            });

            Promise.all(writes).then(function () {
                if (typeof FS.syncfs === 'function') {
                    FS.syncfs(false, function (error) {
                        notify(error ? 'error:persist' : 'ok');
                    });
                } else {
                    notify('ok');
                }
            }).catch(function () {
                notify('error:import');
            });
        });

        input.click();
    }
});
