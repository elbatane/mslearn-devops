$(function() {
    var codeLanguageMap = {
        'language-sh': 'shell',
        'language-js': 'javascript',
        'language-xml': 'xml',
        'language-sql': 'sql',
        'language-csharp': 'c#'
    };

    function copyText(text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            return navigator.clipboard.writeText(text);
        }

        return new Promise(function(resolve, reject) {
            var helper = document.createElement('textarea');
            helper.value = text;
            helper.setAttribute('readonly', '');
            helper.style.position = 'absolute';
            helper.style.left = '-9999px';
            document.body.appendChild(helper);
            helper.select();

            try {
                document.execCommand('copy');
                resolve();
            } catch (error) {
                reject(error);
            } finally {
                document.body.removeChild(helper);
            }
        });
    }

    function setCopyButtonLabel($button, label) {
        $button.empty();

        if (label === 'Copy') {
            $button.append(
                $('<i/>', {
                    class: 'fa fa-files-o mr-2',
                    'aria-hidden': 'true'
                })
            );
        }

        $button.append(document.createTextNode(label));
    }

    var blankTargetMarker = /\{:\s*target\s*=\s*"_blank"\s*\}/;
    $('article a').each(function() {
        var nextNode = this.nextSibling;
        if (!nextNode || nextNode.nodeType !== Node.TEXT_NODE) {
            return;
        }

        var nextText = nextNode.nodeValue || '';
        if (!blankTargetMarker.test(nextText)) {
            return;
        }

        $(this).attr('target', '_blank');

        var rel = ($(this).attr('rel') || '').trim();
        if (!/\bnoopener\b/.test(rel)) {
            rel = (rel ? rel + ' ' : '') + 'noopener';
        }
        if (!/\bnoreferrer\b/.test(rel)) {
            rel = rel + ' noreferrer';
        }
        $(this).attr('rel', rel.trim());

        nextNode.nodeValue = nextText.replace(blankTargetMarker, '');
    });

    $('article img').each(function() {
        var src = $(this).attr('src');
        $(this).wrap(
            $('<a>').attr({
                href: src,
                target: '_blank',
                rel: 'noopener noreferrer'
            })
        );
    });

    $('article > h2').each(function() {
        $('nav.toc > ul').append(
            $('<li>')
                .attr('class', 'nav-item')
                .append(
                    $('<a>')
                        .attr('class', 'nav-link')
                        .text($(this).text())
                        .attr('href', '#' + $(this).attr('id'))
                )
        );
    });

    var $body = $('body');
    var $toc = $('#linksMenu');
    var $tocToggle = $('.toc-toggle');
    var $tocBackdrop = $('.toc-overlay-backdrop');
    var tocOverlayOpenClass = 'toc-overlay-open';

    function closeTocOverlay() {
        $body.removeClass(tocOverlayOpenClass);
        $tocToggle.attr('aria-expanded', 'false');
    }

    function openTocOverlay() {
        $body.addClass(tocOverlayOpenClass);
        $tocToggle.attr('aria-expanded', 'true');
    }

    if ($toc.length && $tocToggle.length && $tocBackdrop.length) {
        $tocToggle.on('click', function(event) {
            event.preventDefault();

            if ($body.hasClass(tocOverlayOpenClass)) {
                closeTocOverlay();
                return;
            }

            openTocOverlay();
        });

        $tocBackdrop.on('click', closeTocOverlay);
        $toc.on('click', 'a.nav-link', closeTocOverlay);

        $(document).on('click', function(event) {
            if (!$body.hasClass(tocOverlayOpenClass)) {
                return;
            }

            if ($(event.target).closest('#linksMenu, .toc-toggle').length) {
                return;
            }

            closeTocOverlay();
        });

        $(window).on('resize orientationchange', closeTocOverlay);
    }

    $('[data-spy="scroll"]').each(function() {
        $(this).scrollspy('refresh');
    });

    $('pre').each(function(index) {
        var generatedId = 'codeBlock' + index;
        var languageClass = (($(this).children('code:first').attr('class')) || '').split(' ')[0];
        var language = codeLanguageMap[languageClass] || 'code';
        $(this).attr('id', generatedId);
        var header = $('<div/>', {
            class: 'code-header mt-3 mb-0 bg-light d-flex justify-content-between border',
        }).append(
            $('<span/>', {
                class: 'mx-2 text-muted text-capitalize font-weight-light',
                text: language
            })
        ).append(
            $('<button/>', {
                class: 'm-0 btn btn-code btn-sm btn-light codeBtn rounded-0 border-left text-muted font-weight-light',
                'data-clipboard-target': '#' + generatedId,
                type: 'button'
            })
        );
        setCopyButtonLabel(header.find('.btn-code'), 'Copy');
        header.insertBefore($(this));
        $(this).addClass('mt-0');
    });

    $(document).on('click', '.btn-code', function() {
        var $button = $(this);
        var target = $button.attr('data-clipboard-target');
        var $source = $(target);

        if (!$source.length) {
            return;
        }

        copyText($source.text()).then(function() {
            setCopyButtonLabel($button, 'Copied');

            window.setTimeout(function() {
                setCopyButtonLabel($button, 'Copy');
            }, 1500);
        }).catch(function() {
            setCopyButtonLabel($button, 'Copy failed');
        });
    });

    if (window.hljs && typeof window.hljs.initHighlightingOnLoad === 'function') {
        window.hljs.initHighlightingOnLoad();
    }
});
