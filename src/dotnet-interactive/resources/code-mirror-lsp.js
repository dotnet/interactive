define({
    init: function (global, document, notebookRoot) {
        // hover display element used by the given `notebookRoot`
        let hoverDiv = document.createElement('div');
        hoverDiv.classList.add('rendered_html');
        hoverDiv.style.backgroundColor = 'white';
        hoverDiv.style.display = 'none';
        hoverDiv.style.position = 'fixed';
        hoverDiv.style.zIndex = 10;
        notebookRoot.appendChild(hoverDiv);

        function createHoverHandler(event, element) {
            return function () {
                // find matching line
                let lines = element.getElementsByClassName("CodeMirror-line");
                let foundLine = false;
                let foundColumn = false;
                let lineNumber = 0;
                for (; lineNumber < lines.length; lineNumber++) {
                    let rect = lines[lineNumber].getBoundingClientRect();
                    if (event.pageX >= rect.left &&
                        event.pageX <= rect.right &&
                        event.pageY >= rect.top &&
                        event.pageY <= rect.bottom) {
                        foundLine = true;
                        break;
                    }
                }

                // find matching column
                let columnNumber = 0;
                if (foundLine) {
                    let childNodes = lines[lineNumber].children[0].childNodes;
                    for (let childNode of childNodes) {
                        let childRect = null;
                        let childText = "";
                        if (childNode.nodeType === 1) {
                            // DOM element
                            childRect = childNode.getBoundingClientRect();
                            childText = childNode.innerText;
                        } else if (childNode.nodeType === 3) {
                            // text
                            let range = document.createRange();
                            range.selectNode(childNode);
                            childRect = range.getBoundingClientRect();
                            childText = childNode.textContent;
                            range.detach();
                        }

                        if (childRect) {
                            if (event.pageX >= childRect.left &&
                                event.pageX <= childRect.right) {
                                foundColumn = true;
                                break;
                            }
                        }

                        columnNumber += childText.length;
                    }
                }

                if (foundColumn) {
                    global.Lsp.textDocumentHover(element, lineNumber, columnNumber).then(function (result) {
                        hoverDiv.style.top = `${event.pageY}px`;
                        hoverDiv.style.left = `${event.pageX}px`;
                        hoverDiv.style.display = 'inline';
                        if (result.contents.kind === "markdown") {
                            require(['components/marked/lib/marked'], function (marked) {
                                // this should work for jupyter notebook
                                hoverDiv.innerHTML = marked(result.contents.value);
                            }, function (_err) {
                                // couldn't load `marked`; this will happen in jupyter lab
                                let pre = document.createElement('pre');
                                pre.innerText = result.contents.value;
                                hoverDiv.innerHTML = '';
                                hoverDiv.appendChild(pre);
                            });
                        } else {
                            hoverDiv.innerText = result.contents.value;
                        }
                    });
                }
            };
        }

        function applyHoverTimeoutsToElement(element) {
            if (!element._hasCodeMirrorEvents) {
                element._hasCodeMirrorEvents = true;
                element._hoverTimeoutHandle = 0;
                element._clearHoverTimeout = function () {
                    if (element._hoverTimeoutHandle) {
                        clearTimeout(element._hoverTimeoutHandle);
                        element._hoverTimeoutHandle = 0;
                    }
                };
                element._setHoverTimeout = function (event) {
                    hoverDiv.style.display = 'none';
                    element._clearHoverTimeout();
                    element._hoverTimeoutHandle = setTimeout(createHoverHandler(event, element), 500);
                };
                element.addEventListener('mousemove', (e) => {
                    element._setHoverTimeout(e);
                });
                element.addEventListener('mouseleave', () => {
                    element._clearHoverTimeout();
                });
            }
        }

        function isCodeMirrorEditor(element) {
            return element.nodeName.toLowerCase() === 'div' && element.classList.contains('CodeMirror');
        }

        // find all extant CodeMirror editors and enable the appropriate events
        let extantEditors = notebookRoot.getElementsByClassName('CodeMirror');
        for (let extantEditor of extantEditors) {
            applyHoverTimeoutsToElement(extantEditor);
        }

        // ensure all subsequently created editors are subscribed
        let observer = new MutationObserver(function (mutationList) {
            for (let mutation of mutationList) {
                switch (mutation.type) {
                    case 'attributes':
                        if (isCodeMirrorEditor(mutation.target)) {
                            applyHoverTimeoutsToElement(mutation.target);
                        }
                        break;
                    case 'childList':
                        for (let added of mutation.addedNodes) {
                            if (isCodeMirrorEditor(added)) {
                                applyHoverTimeoutsToElement(added);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        });
        observer.observe(notebookRoot, {
            attributes: true,
            attributeFilter: [
                'class'
            ],
            childList: true,
            subtree: true
        });
    }
});
