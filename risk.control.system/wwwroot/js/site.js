
const MaxSizeInBytes = 5242880; // 5MG for upload

document.addEventListener("DOMContentLoaded", function () {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    })
    const addressInput = document.getElementById("Addressline");
    if (addressInput) {
        addressInput.addEventListener("input", function () {
            if (this.value.length > 0) {
                this.value = this.value.replace(/\b\w/g, function (char) {
                    return char.toUpperCase();
                });
            }
        });
    }
});

document.addEventListener("DOMContentLoaded", function () {
    var printButton = document.getElementById("printInvoiceButton");

    if (printButton) {
        printButton.addEventListener("click", function (event) {
            event.preventDefault(); // Prevent default link behavior
            window.print(); // Trigger the print dialog
        });
    }

    var closeButton = document.getElementById("close-button");
    if (closeButton) {
        closeButton.addEventListener("click", function () {
            window.close();
        });
    }

});

function checkFormCompletion(formSelector, create = false) {
    let isFormComplete = true;

    // Check all required fields (select, input fields, and file inputs)
    $(formSelector).find('select[required], input[required], input[type="file"]').each(function () {
        const fieldType = $(this).attr('type');

        // Skip file input validation in edit mode
        if (fieldType === 'file' && !create) {
            return; // Skip to the next field
        }

        // Check if the field has a value
        var inputValue = $(this).val();
        if (!inputValue) {
            isFormComplete = false;
            return false; // Exit loop early if a required field is empty
        }

        //Check email address
        const emailAddress = document.getElementById("emailAddress");
        if (emailAddress) {
            const emailData = emailAddress.value;
            if (!emailData) {
                isFormComplete = false;
                return false; // Exit loop early if a required field is empty
            }
            const resultSpan = document.querySelector('#result span');
            if (!resultSpan.classList.contains('available')) {
                isFormComplete = false;
                return false; // Exit loop early if a required field is empty
            } 
        }

        //Check phone number
        const phone = $('#PhoneNumber');
        if (phone && phone.hasClass("is-invalid")) {
            isFormComplete = false;
            return false; // Exit loop early if a required field is empty
        }
        // Validate file input type in create mode
        if (fieldType === 'file' && create) {
            const allowedExtensions = ['jpg', 'png', 'jpeg']; // Define your allowed extensions here
            if (!validateFileInput(this, allowedExtensions)) {
                isFormComplete = false;
                return false; // Exit loop if the file type is invalid
            }
        }
    });

    // Additional check for PinCodeId field
    if ($('#PinCodeText').length > 0 && ($('#PinCodeText').val() || []).length === 0) {
        isFormComplete = false;
    }
    // Enable or disable the submit button
    $(formSelector).find('button[type="submit"]').prop('disabled', !isFormComplete);
}
function validateFileInput(inputElement, allowedExtensions) {
    var MaxSizeInBytes = 5242880;
    if (!inputElement.files || !inputElement.files[0]) {
        return false; // Exit early if no files are selected
    }

    const file = inputElement.files[0];
    var fileSize = file.size;

    const fileExtension = file.name.split('.').pop().toLowerCase();

    if (!allowedExtensions.includes(fileExtension)) {
        inputElement.value = ''; // Clear the input

        $.alert({
            title: "FILE UPLOAD TYPE !!",
            content: `Pls select only image with extension ${allowedExtensions.join(', ')} ! `,
            icon: 'fas fa-exclamation-triangle',
            type: 'red',
            closeIcon: true,
            buttons: {
                cancel: {
                    text: "CLOSE",
                    btnClass: 'btn-danger'
                }
            }
        });
    }
    if (fileSize > MaxSizeInBytes) {
        document.getElementById('document-Image').src = '/img/no-image.png';
        document.getElementById('createImageInput').value = '';
        $.alert({
            title: "Image UPLOAD issue !",
            content: " <i class='fa fa-upload'></i> Upload Image size limit exceeded. <br />Max file size is 5 MB!",
            icon: 'fas fa-exclamation-triangle',
            type: 'red',
            closeIcon: true,
            buttons: {
                cancel: {
                    text: "CLOSE",
                    btnClass: 'btn-danger'
                }
            }
        });
    } else {
        document.getElementById('document-Image').src = window.URL.createObjectURL(file);
    }
    return true;
}
function validateInput(selector, regex) {
    $(selector).on('input', function () {
        const value = $(this).val();
        // Remove invalid characters directly using the regex
        $(this).val(value.replace(regex, ''));
    });
}
function openForm() {
    document.getElementById("myForm").removeClass("hidden-section");
}
function closeForm() {
    document.getElementById("myForm").addClass("hidden-section");
}
function clearAllInputs(event) {
    var allInputs = document.querySelectorAll('input');

    allInputs.forEach(singleInput => singleInput.readOnly ? singleInput.value : singleInput.value = '');
    $("option:selected").prop("selected", false);
    var companyImage = document.getElementById('companyImage');
    if (companyImage) {
        companyImage.src = '/img/no-image.png';
    }
    var policyImage = document.getElementById('policyImage');
    var profileImage = document.getElementById('profileImage');
    if (policyImage) {
        policyImage.src = '/img/no-policy.jpg';
    }
    if (profileImage) {
        profileImage.src = '/img/no-user.png';
    }
}

function error(err) {
    console.error('Geolocation request failed or was denied:', err.message);
    displayUnavailableInfo();
}

async function fetchIpInfo() {
    try {
        const parser = new UAParser();
        const browserInfo = parser.getResult();
        updateInfoDisplay({
            browser: `${browserInfo.browser.name?.toLowerCase()} ${browserInfo.browser.major}` || 'Not available',
            device: getDeviceType() || 'Not available',
            os: `${browserInfo.os.name?.toLowerCase()} ${browserInfo.os.version}` || 'Not available',
        });
    } catch (err) {
        console.error('Error during IP info fetch operation:', err.message);
        displayUnavailableInfo();
    }
}

function updateInfoDisplay(info) {
    const fields = {
        browser: '#browser .info-data',
        device: '#device .info-data',
        os: '#os .info-data',
    };

    for (const [key, selector] of Object.entries(fields)) {
        const element = document.querySelector(selector);
        if (element) element.textContent = info[key];
    }
}

function displayUnavailableInfo() {
    updateInfoDisplay({
        browser: 'Not available',
        device: 'Not available',
        os: 'Not available',
    });
}

function getDeviceType() {
    const ua = navigator.userAgent;
    if (/(tablet|ipad|playbook|silk)|(android(?!.*mobi))/i.test(ua)) {
        return "tablet";
    }
    if (
        /Mobile|iP(hone|od)|Android|BlackBerry|IEMobile|Kindle|Silk-Accelerated|(hpw|web)OS|Opera M(obi|ini)/.test(
            ua
        )
    ) {
        return getMobileType();
    }
    return "desktop";
};
function getMobileType() {
    if (/Android/i.test(navigator.userAgent)) {
        return 'android';
    } else if (/iPhone|iPad|iPod/i.test(navigator.userAgent)) {
        return 'iOS';
    } else if (/Windows Phone/i.test(navigator.userAgent)) {
        return 'windows phone';
    } else {
        return 'other';
    }
}

var moreInfo = "...";

function markNotificationAsRead(notificationId) {
    var token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/api/Notification/MarkAsRead',
        type: 'POST',
        headers: {
            "X-CSRF-TOKEN": token,
        },
        contentType: 'application/json', // Specify JSON format
        data: JSON.stringify({ Id: notificationId }), // Convert data to JSON
        success: function () {
            $("#notificationDropdown").addClass("show");
            $("#notificationToggle").attr("aria-expanded", "true");
            loadNotifications(true);
            console.log("Notification marked as read:", notificationId);
            $("#notificationDropdown").addClass("show");
            $("#notificationToggle").attr("aria-expanded", "true");
        },
        error: function (xhr) {
            console.error("Error:", xhr.responseText);
        }
    });
}
function loadNotifications(keepOpen = false) {
    var noNotification = $('#no-notification').val();
    if (noNotification =='true') {
        return;
    }
    $.get('/api/Notification/GetNotifications', function (response) {

        const $list = $("#notificationList");
        $list.empty();

        // Safe count display
        const totalCount = response.total;
        $("#notificationCount").text(
            response.maxCountReached ? `${response.maxCount}+` : totalCount
        );

        if (Array.isArray(response.data) && response.data.length > 0) {

            response.data.forEach(function (item) {

                // ===== SANITIZE ALL REMOTE DATA (Very important) =====

                const safeMessage = sanitizeText(item.message);
                const safeStatus = sanitizeText(item.status);
                const safeCreatedAt = sanitizeDate(item.createdAt);
                const safeIconClass = sanitizeCssClass(item.symbol);

                // ===== BUILD DOM SAFELY (No HTML strings) =====

                const $a = $("<a>")
                    .addClass("notification-item")
                    .attr("href", "#")
                    .attr("data-id", item.id);

                const $icon = $("<i>").addClass(safeIconClass);

                const $message = $("<span>")
                    .addClass("notification-message text-muted text-xs")
                    .text(safeMessage);

                const $status = $("<span>")
                    .addClass("badge badge-light text-muted text-xs")
                    .text(safeStatus);

                // Clock text + icon (no HTML strings)
                const $time = $("<span>")
                    .addClass("notification-time text-muted text-xs");

                const $clockIcon = $("<i>").addClass("far fa-clock");
                const $created = $("<span>").text(" " + safeCreatedAt);
                $time.append($clockIcon).append($created);

                // Delete button
                const $delete = $("<span>")
                    .addClass("delete-notification")
                    .attr("data-id", item.id)
                    .append($("<i>").addClass("fas fa-trash"));

                // Build content
                const $content = $("<div>")
                    .addClass("notification-content")
                    .append($icon)
                    .append($message)
                    .append($status)
                    .append(
                        $("<div>")
                            .addClass("notification-action-content float-right")
                            .append($time)
                            .append($delete)
                    );

                $a.append($content);
                $list.append($a);
            });

            $("#clearNotifications").removeClass("clear-disabled");

        } else {

            $list.append(
                $("<div>")
                    .addClass("text-center text-muted")
                    .text("No notifications")
            );

            $("#clearNotifications").addClass("clear-disabled");
        }

        // ===== DELETE HANDLER SAFE ATTACH =====

        $(".delete-notification").off("click").on("click", function (e) {
            e.stopPropagation();

            const $this = $(this);

            // Keep dropdown open
            $("#notificationDropdown").addClass("show");
            $("#notificationToggle").attr("aria-expanded", "true");

            $this.addClass("fa-spin");

            const notificationId = $this.data("id");
            markNotificationAsRead(notificationId);

            setTimeout(() => $this.removeClass("fa-spin"), 1000);
        });

        // ===== KEEP DROPDOWN OPEN IF NEEDED =====

        if (keepOpen) {
            $("#notificationDropdown").addClass("show");
            $("#notificationToggle").attr("aria-expanded", "true");
        }
    });
}
function sanitizeText(str) {
    if (!str) return "";
    return String(str).replace(/[<>]/g, ""); // Remove tags entirely
}
function sanitizeDate(str) {
    if (!str) return "";
    return String(str).replace(/[^0-9A-Za-z:\-\/\s]/g, "");
}
function sanitizeCssClass(str) {
    if (!str) return "";
    return String(str).replace(/[^a-zA-Z0-9\-\s]/g, "");
}
function clearAllNotifications() {
    var token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: '/api/Notification/ClearAll', // Backend endpoint to clear notifications
        type: 'POST',
        headers: {
            "X-CSRF-TOKEN": token,
        },
        success: function () {
            $("#notificationList").html('<div class="text-muted text-center">No notifications</div>');
            $("#notificationCount").text("0");
            console.log("All notifications cleared.");
            // Keep dropdown open after clearing
            $("#notificationDropdown").addClass("show");
            $("#notificationToggle").attr("aria-expanded", "true");
        },
        error: function (xhr) {
            console.error("Error clearing notifications:", xhr.responseText);
        }
    });
}

$(document).ready(function () {
    $("#PhoneNumber").on("keydown", function (e) {
        // Prevent first character being 0 (also covers numpad 0)
        if (this.selectionStart === 0 && (e.key === "0" || e.code === "Numpad0")) {
            e.preventDefault();
        }
    });

    $("#PhoneNumber").on("paste", function (e) {
        let pasteData = (e.originalEvent || e).clipboardData.getData('text');

        // If the pasted text starts with 0, block it or fix it
        if (pasteData.startsWith("0")) {
            e.preventDefault();

            // Option 1: strip leading zeros automatically
            let cleaned = pasteData.replace(/^0+/, "");
            if (cleaned.length > 0) {
                document.execCommand("insertText", false, cleaned);
            }

            // Option 2 (stricter): block paste completely
            // e.preventDefault();
        }
    });

    // Prevent typing first character as 0
    $("#PhoneNumber").on("input", function () {
        let val = $(this).val();
        if (val.length === 1 && val === "0") {
            $(this).val(""); // clear the input
        }
    });
    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });

    $(".delete-notification").on("click", function (e) {
        e.stopPropagation(); // Prevent closing the dropdown
        $("#notificationDropdown").addClass("show");
        $("#notificationToggle").attr("aria-expanded", "true");
        $(this).addClass("fa-spin");
        var notificationId = $(this).data("id");
        markNotificationAsRead(notificationId);
        setTimeout(() => $(this).removeClass("fa-spin"), 1000);
    });

    $("#notification-refresh").on("click", function (e) {
        e.stopPropagation(); // Prevent Bootstrap from closing the dropdown
        $("#notificationDropdown").addClass("show");
        $("#notificationToggle").attr("aria-expanded", "true");
        $(this).addClass("fa-spin");

        loadNotifications(true); // Reload notifications & keep open

        setTimeout(() => $(this).removeClass("fa-spin"), 1000);
    });

    $("#clearNotifications").on("click", function (e) {
        if ($(this).hasClass("clear-disabled")) return; // Prevent action when disabled

        e.stopPropagation(); // Prevent Bootstrap from closing the dropdown
        $("#notificationDropdown").addClass("show");
        $("#notificationToggle").attr("aria-expanded", "true");
        $(this).addClass("fa-spin");

        clearAllNotifications(); // Reload notifications & keep open

        setTimeout(() => $(this).removeClass("fa-spin"), 1000);
    });
    // Close dropdown when clicking outside
    $(document).on("click", function (event) {
        if (!$(event.target).closest("#notificationDropdown, #notificationToggle").length) {
            $("#notificationDropdown").removeClass("show");
            $("#notificationToggle").attr("aria-expanded", "false");
        }
    });
    $('.print-me').on('click', function () {
        window.print();
        return false;
    });
    $('.close-myForm').on('click', function () {
        closeForm();
    });
    $('#open-myForm').on('click', function () {
        openForm();
    });
    $('[data-toggle="tooltip"]').tooltip({
        animated: 'fade',
        placement: 'top',
        html: true
    });

    $('#logout').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#logout').attr('disabled', 'disabled');
        $('#logout').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Logout");
        $('#logoutForm').submit();
        var content = document.getElementById("main-content");
        if (content) {
            var nodes = content.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('#cancel').on('click', function (e) {

        const url = $(this).attr('href');
        e.preventDefault(); // Prevent the default navigation
        $.confirm({
            title: 'Confirm Cancel',
            content: 'Any unsaved changes will be lost.',
            icon: 'fas fa-exclamation-triangle',
            type: 'orange',
            buttons: {
                confirm: {
                    text: 'Yes, Cancel',
                    btnClass: 'btn-warning',
                    action: function () {
                        disableAllInteractiveElements();
                        refreshSession();
                        window.location.href = url;
                    }
                },
                cancel: {
                    text: 'No, Stay',
                    btnClass: 'btn-secondary'
                }
            }
        });
    });

    $('#back').on('click', function () {
        refreshSession();

        disableAllInteractiveElements();
    });

    $('.nav-item a.actual-link,.nav-item a.navlink-border, a.details-page').on('click', function (e) {
        if (!window.matchMedia("(max-width: 767px)").matches) {
            $("body").addClass("submit-progress-bg");
            // Wrap in setTimeout so the UI
            // can update the spinners
            setTimeout(function () {
                $(".submit-progress").removeClass("hidden");
            }, 1);
        }

        refreshSession();
        $('a, button').css('cursor', 'not-allowed');
        $('a, button').attr('disabled', 'disabled');

        var section = document.getElementById("section");
        if (section) {
            var nodes = section.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    })

    $("#datepicker").datepicker({ maxDate: '0' });

    $('.row-links').on('click', function () {
        let form = $('#message-detail');
        form.submit();
    });

    $('tbody tr').on('click', function () {
        let id = $(this).data('url');
        if (typeof id !== 'undefined') {
            window.location.href = id;
        }
    });

    $("#checkall").click(function () {
        var status = $("#checkall").prop('checked');
        $('#manage-vendors').prop('disabled', !status)
        toggleChecked(status);
    });

    $("input.vendors").click(function () {
        var checkboxes = $("input[type='checkbox'].vendors");
        var anyChecked = checkIfAnyChecked(checkboxes);
        var allChecked = checkIfAllChecked(checkboxes);
        $('#checkall').prop('checked', allChecked);
        $('#manage-vendors').prop('disabled', !anyChecked)
    });

});
function disableAllInteractiveElements() {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);

    // Disable buttons and inputs
    $('button, input[type="submit"]').prop('disabled', true);

    // Add visual disabled state for anchors
    $('a').addClass('disabled-anchor').on('click', function (e) {
        e.preventDefault();
    });

    // Apply CSS to make interactive elements unclickable
    $('button, a, .text').addClass('disable-elements');
}
function checkIfAllChecked(elements) {
    var totalElmentCount = elements.length;
    var totalCheckedElements = elements.filter(":checked").length;
    return (totalElmentCount == totalCheckedElements)
}
function checkIfAnyChecked(elements) {
    var hasAnyCheckboxChecked = false;

    $.each(elements, function (index, element) {
        if (element.checked === true) {
            hasAnyCheckboxChecked = true;
        }
    });
    return hasAnyCheckboxChecked;
}
function toggleChecked(status) {
    $("#checkboxes input").each(function () {
        // Set the checked status of each to match the
        // checked status of the check all checkbox:
        $(this).prop("checked", status);
    });
}
function DisableBackButton() {
    window.history.forward()
}
DisableBackButton();
window.onload = DisableBackButton;
window.onpageshow = function (evt) { if (evt.persisted) DisableBackButton() }

fetchIpInfo();

loadNotifications(false);


let internetPopup = null;
function checkInternetConnection() {
    if (!navigator.onLine) {

        // If popup already exists, do not open another
        if (internetPopup) return;

        internetPopup = $.confirm({
            title: 'No Internet Connection',
            content: 'It looks like your internet connection is down. Please check and try again.',
            type: 'red',
            closeIcon: false,
            buttons: {
                tryAgain: {
                    text: 'Retry',
                    action: function () {
                        internetPopup = null; // reset before retry
                        checkInternetConnection();
                        return false; // keep dialog open if still offline
                    }
                },
                close: function () {
                    internetPopup = null;
                }
            },
            onDestroy: function () {
                internetPopup = null;
            }
        });
    }
    else {
        // If connection is back, close popup automatically
        if (internetPopup) {
            internetPopup.close();
            internetPopup = null;
        }
    }
}
window.addEventListener('online', function () {
    internetPopupOpen = false;
    $('.jconfirm').remove();
});

