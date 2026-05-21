$(function() {
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
        $(this).wrap('<a href="' + src + '" target="_blank"></a>');
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
        var languageClass = $(this).children('code:first').attr('class').split(' ')[0];
        var language = languageClass == 'language-sh' ? 'shell' :
            languageClass == 'language-js' ? 'javascript' :
            languageClass == 'language-xml' ? 'xml' :
            languageClass == 'language-sql' ? 'sql' :
            languageClass == 'language-csharp' ? 'c#' : 'code';
        $(this).attr('id', generatedId);
        var header = $('<div/>', {
            class: 'code-header mt-3 mb-0 bg-light d-flex justify-content-between border',
        }).append(
            $('<span/>', {
                class: 'mx-2 text-muted text-capitalize font-weight-light',
                html: language
            })
        ).append(
            $('<button/>', {
                class: 'm-0 btn btn-code btn-sm btn-light codeBtn rounded-0 border-left text-muted font-weight-light',
                'data-clipboard-target': '#' + generatedId,
                type: 'button'
            }).append(
                $('<i/>', {
                    class: 'fa fa-files-o mr-2',
                    'aria-hidden': 'true'
                })
            ).append(
                'Copy'
            )
        );
        header.insertBefore($(this));
        $(this).addClass('mt-0');
    });

    hljs.initHighlightingOnLoad();
    new ClipboardJS('.btn-code');
});
