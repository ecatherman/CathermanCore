// ─────────────────────────────────────────────────────────────────────────────
// cathermanCore.js: helpers with no knowledge of any particular app.
// Needs jQuery (and Bootstrap for the alerts, TinyMCE for the editor helpers) at call time.
// Load it before the app's own script:
//   <script src="~/_content/CathermanCore.Web/cathermanCore.js" asp-append-version="true"></script>
// ─────────────────────────────────────────────────────────────────────────────

function filterInPlace(array, callbackFn) {
    let writeIndex = 0; // Index for where to write elements that pass the filter
    for (let readIndex = 0; readIndex < array.length; readIndex++) {
        if (callbackFn(array[readIndex], readIndex, array)) {
            array[writeIndex] = array[readIndex];
            writeIndex++;
        }
    }
    // Truncate the array to remove elements that did not pass the filter
    array.length = writeIndex;
}

Object.defineProperty(String.prototype, 'capitalize', {
    value: function () {
        return String(this).replace(/\b\w/g, char => char.toUpperCase());
    },
    enumerable: false // Keeps it hidden during loops
});

// Used to avoid the warning for putting JSON data into javascript with a Razor call to serialize data
function set(value) { return value; }

// Used by the modal tag helpers (CathermanCore.Web) to stack on top of whatever is already showing
function getTopZIndex() {
    let max = 0;
    for (const element of document.body.querySelectorAll('*')) {
        const zIndex = parseFloat(window.getComputedStyle(element).zIndex);
        if (zIndex > max) // false for NaN, which is what "auto" parses to
            max = zIndex;
    }
    return max;
}

function getTinyMceValue(id) {
    try {
        return tinymce.get(id)?.getContent(); // undefined (not '') when there is no editor, so .val() leaves the textarea alone
    }
    catch {
        return '';
    }
}

// Posts to a Razor Pages handler with the antiforgery token that the page's form provides
function postWithToken(url) {
    return $.ajax({
        url: url,
        type: 'POST',
        headers: {
            "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val(),
            'Content-Type': 'application/x-www-form-urlencoded'
        }
    });
}

// ─────────────────────────────────────────────────────────────────────────────
// Floating alerts
// ─────────────────────────────────────────────────────────────────────────────

//generated with chatGPT https://chatgpt.com/c/68ffb7f6-3774-8333-8d6a-6af1abd1fc7a
function showFloatingAlert(message, type = 'success', timeout = 4000) {
    const alertId = 'floating-alert-' + Date.now();

    // Create the alert element (detached from any container)
    const $alert = $(`
    <div id="${alertId}"
            class="alert alert-${type} floating-alert alert-dismissible fade show shadow-lg"
            role="alert"
            style="
            position: fixed;
            top: ${(document.querySelectorAll('.floating-alert').length * 70) + 20}px;
            left: 50%;
            transform: translateX(-50%);
            z-index: 2000;
            min-width: 300px;
            max-width: 600px;
            text-align: center;
            opacity: 0;
            transition: opacity 0.5s ease;
            ">
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
    `);

    // prepend, not .text(): .text() would replace the close button along with everything else
    $alert.prepend(document.createTextNode(message));

    // Append directly to the <body>
    $('body').append($alert);

    // Wait one tick to trigger the CSS transition
    requestAnimationFrame(() => {
        $alert.css('opacity', '1');
    });

    // Auto-dismiss after a few seconds, unless the mouse is over it
    if (timeout > 0) {
        const startDismissTimer = () => setTimeout(() => {
            $alert.css('opacity', '0'); // fade out smoothly
            setTimeout(() => $alert.remove(), 500); // match fade duration
        }, timeout);

        let dismissTimer = startDismissTimer();

        $alert.on('mouseenter', () => clearTimeout(dismissTimer));
        $alert.on('mouseleave', () => {
            clearTimeout(dismissTimer);
            dismissTimer = startDismissTimer();
        });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Images pasted into TinyMCE
// ─────────────────────────────────────────────────────────────────────────────

const maxImageDimension = 1200;     // px, longest side
const maxImageBytes = 600 * 1024;   // base64 adds about a third. Keep the result under whatever the server accepts (BibleWork: HtmlService's 1,000,000 characters)

// Shrinks an image blob to a JPEG data URI. Re-encoding through a canvas also drops the EXIF data.
function resizeImageBlob(blob) {
    return new Promise((resolve, reject) => {
        const objectUrl = URL.createObjectURL(blob);
        const img = new Image();

        img.onload = () => {
            URL.revokeObjectURL(objectUrl);

            const scale = Math.min(1, maxImageDimension / Math.max(img.naturalWidth, img.naturalHeight));
            const canvas = document.createElement('canvas');
            canvas.width = Math.round(img.naturalWidth * scale);
            canvas.height = Math.round(img.naturalHeight * scale);

            const context = canvas.getContext('2d');
            context.fillStyle = '#ffffff';          // JPEG has no transparency
            context.fillRect(0, 0, canvas.width, canvas.height);
            context.drawImage(img, 0, 0, canvas.width, canvas.height);

            for (const quality of [0.85, 0.7, 0.55]) {
                const dataUrl = canvas.toDataURL('image/jpeg', quality);
                if (dataUrl.length * 0.75 <= maxImageBytes) {
                    resolve({ dataUrl, width: canvas.width, height: canvas.height });
                    return;
                }
            }
            reject({ message: 'That image is too large, even after attempting to shrink it. Please use a smaller one.', remove: true });
        };

        img.onerror = () => {
            URL.revokeObjectURL(objectUrl);
            reject({ message: 'That file could not be read as an image.', remove: true });
        };

        img.src = objectUrl;
    });
}