(g => { var h, a, k, p = "API", c = "google", l = "importLibrary", q = "__ib__", m = document, b = window; b = b[c] || (b[c] = {}); var d = b.maps || (b.maps = {}), r = new Set, e = new URLSearchParams, u = () => h || (h = new Promise(async (f, n) => { await (a = m.createElement("script")); e.set("libraries", [...r] + ""); for (k in g) e.set(k.replace(/[A-Z]/g, t => "_" + t[0].toLowerCase()), g[k]); e.set("callback", c + ".maps." + q); a.src = `https://maps.${c}apis.com/maps/api/js?` + e; d[q] = f; a.onerror = () => h = n(Error(p + " could not load.")); a.nonce = m.querySelector("script[nonce]")?.nonce || ""; m.head.append(a) })); d[l] ? console.warn(p + " only loads once. Ignoring:", g) : d[l] = (f, ...n) => r.add(f) && u().then(() => d[l](f, ...n)) })
    ({ key: "AIzaSyCYPyGotbPJAcE9Ap_ATSKkKOrXCQC4ops", v: "beta" });

let mapz;
var showCustomerMap = false;
var showBeneficiaryMap = false;
var showFaceMap = false;
var showLocationMap = false;
var showOcrMap = false;
const image =
    "/images/beachflag.png";

// Function to trigger the print dialog
//function printInvoice() {
//    window.print();  // Opens the print dialog
//    return false;
//}

//document.addEventListener("DOMContentLoaded", function () {
//    // Apply blur effect dynamically
//    document.getElementById("main-container").classList.add("blur-background");
//});
// Add event listener to the print button once the DOM is fully loaded

function loadNotifications() {
    $.get('/api/Notification/GetNotifications', function (data) {
        $("#notificationList").html("");
        $("#notificationCount").text(data.length);

        data.forEach(function (item) {
            $("#notificationList").append(
                `<a href="#" class="dropdown-item notification-item" data-id="${item.id}">
                            <i class="fas fa-tasks mr-2 badge bage-light"></i> ${item.message}
                            <span class="float-right text-muted text-sm">${item.createdAt}</span>
                        </a>`
            );
        });

        // Click event to mark as read
        $(".notification-item").on("click", function () {
            var notificationId = $(this).data("id");
            markNotificationAsRead(notificationId);
            $(this).remove(); // Remove notification from UI
            let count = parseInt($("#notificationCount").text()) - 1;
            $("#notificationCount").text(count > 0 ? count : "0");
        });
    });
}

function markNotificationAsRead(notificationId) {
    $.ajax({
        url: '/api/Notification/MarkAsRead',
        type: 'POST',
        contentType: 'application/json', // Specify JSON format
        data: JSON.stringify({ Id: notificationId }), // Convert data to JSON
        success: function () {
            console.log("Notification marked as read:", notificationId);
        },
        error: function (xhr) {
            console.error("Error:", xhr.responseText);
        }
    });
}

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
        if (!$(this).val()) {
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
    if ($('#PinCodeId').length > 0 && ($('#PinCodeId').val() || []).length === 0) {
        isFormComplete = false;
    }

    // Enable or disable the submit button
    $(formSelector).find('button[type="submit"]').prop('disabled', !isFormComplete);
}

// Function to validate file input types
function validateFileInput(inputElement, allowedExtensions) {
    var MaxSizeInBytes = 2097152;
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
        document.getElementById('createProfileImage').src = '/img/no-image.png';
        document.getElementById('createImageInput').value = '';
        $.alert({
            title: "Image UPLOAD issue !",
            content: " <i class='fa fa-upload'></i> Upload Image size limit exceeded. <br />Max file size is 2 MB!",
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
        document.getElementById('createProfileImage').src = window.URL.createObjectURL(file);
    }
    return true;
}



// Generic input validation function
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
        //if (!latlong) throw new Error("Latitude and longitude are not available");

        //const url = `/api/Notification/GetClientIp?url=${encodeURIComponent(window.location.pathname)}&latlong=${encodeURIComponent(latlong)}`;
        const parser = new UAParser();
        const browserInfo = parser.getResult();

        //const response = await fetch(url);
        //if (!response.ok) {
        //    console.error(`IP fetch failed with status: ${response.status}`);
        //    displayUnavailableInfo();
        //    return;
        //}

        //const data = await response.json();
        updateInfoDisplay({
            //ipAddress: data.ipAddress || 'Not available',
            //city: data.district || 'Not available',
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
        //ipAddress: '#ipAddress .info-data',
        //city: '#city .info-data',
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
        //ipAddress: 'Not available',
        //city: 'Not available',
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

$(document).ready(function () {
    loadNotifications();
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

    $('.nav-item a.actual-link,.nav-item a.navlink-border').on('click', function (e) {
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

    // delete messages
    $('#delete-messages').on('click', function () {
        let ids = [];
        let form = $('#listForm');
        let checkboxArray = document.getElementsByName('ids');

        // check if checkbox is checked
        for (let i = 0; i < checkboxArray.length; i++) {
            if (checkboxArray[i].checked)
                ids.push(checkboxArray[i].value);
        }

        // submit form
        if (ids.length > 0) {
            if (confirm("Are you sure you want to delete this item(s)?")) {
                form.submit();
            }
        }
    });

    $('#delete-message').on('click', function () {
        $('#deleteForm').submit();
    });

    // Attach the call to toggleChecked to the
    // click event of the global checkbox:
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

    $('.face-Image').click(function () {
        var data;
        $.confirm({
            type: 'green',
            closeIcon: true,

            buttons: {
                confirm: {
                    text: "Ok",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/ClaimsInvestigation/GetInvestigationFaceIdData?claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    data = response;
                    self.setTitle('<i class="fas fa-portrait"></i> Photo <span class="badge badge-light">uploaded</span>');
                    self.setContent('<span class="badge badge-light"><i class="far fa-image"></i> Photo Scanned Image</span>');
                    self.setContentAppend('<br><img id="agentLocationPicture" class="img-fluid investigation-actual-image" src="' + response.location + '" /> ');
                    self.setContentAppend('<br><span class="badge badge-light"><i class="fas fa-info"></i> Location Info</span> ');
                    self.setContentAppend('<br><i>' + response.locationData + '</i>');
                    showFaceMap = true;
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            },
            onContentReady: function () {

            }
        })
    })

    $('.locationImage').click(function () {
        var data;
        $.confirm({
            type: 'green',
            closeIcon: true,
            columnClass: 'medium',

            buttons: {
                confirm: {
                    text: "Ok",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/ClaimsInvestigation/GetInvestigationFaceIdData?claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    data = response;
                    self.setTitle('<i class="fas fa-portrait"></i> Photo <span class="badge badge-light">Uploaded location</span>');
                    self.setContent('<span class="badge badge-light"><i class="fas fa-map-pin"></i> Location visited</span>:');
                    self.setContentAppend('<div id="maps"></div>')
                    self.setContentAppend('<br><div id="pop-map"></div>')
                    self.setContentAppend('</div>')
                    self.setContentAppend('<span class="badge badge-light"><i class="fas fa-map-marker-alt"></i> Address visited</span>:');
                    self.setContentAppend('<br><i>' + response.imageAddress + '</i>');
                    showLocationMap = true;
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            },
            onContentReady: function () {
                if (showLocationMap) {
                    showLocationMap = false;
                    initPopMap(data.facePosition, data.imageAddress);
                }
            }
        })
    })

    $('.olocationImage').click(function () {
        var data;
        $.confirm({
            type: 'green',
            closeIcon: true,
            columnClass: 'medium',

            buttons: {
                confirm: {
                    text: "Ok",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/ClaimsInvestigation/GetInvestigationPanData?claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    data = response;
                    self.setTitle('<i class="fas fa-portrait"></i> Pan card <span class="badge badge-light">uploaded location</span>');
                    self.setContent('<span class="badge badge-light"><i class="fas fa-map-pin"></i> Location visited</span>:');
                    self.setContentAppend('<div id="maps"></div>')
                    self.setContentAppend('<br><div id="pop-map"></div>')
                    self.setContentAppend('</div>')
                    self.setContentAppend('<span class="badge badge-light"><i class="fas fa-map-marker-alt"></i> Address visited</span>:');
                    self.setContentAppend('<br><i>' + response.ocrAddress + '</i>');
                    showOcrMap = true;
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            },
            onContentReady: function () {
                if (showOcrMap) {
                    showOcrMap = false;
                    initPopMap(data.ocrPosition, data.ocrAddress);
                }
            }
        })
    })

    $('.passportlocationImage').click(function () {
        var data;
        $.confirm({
            type: 'green',
            closeIcon: true,
            columnClass: 'medium',

            buttons: {
                confirm: {
                    text: "Ok",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/ClaimsInvestigation/GetInvestigationPassportData?claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    data = response;
                    self.setTitle('<i class="fas fa-portrait"></i> Passport <span class="badge badge-light">uploaded location</span>');
                    self.setContent('<span class="badge badge-light"><i class="fas fa-map-pin"></i> Location visited</span>:');
                    self.setContentAppend('<div id="maps"></div>')
                    self.setContentAppend('<br><div id="pop-map"></div>')
                    self.setContentAppend('</div>')
                    self.setContentAppend('<span class="badge badge-light"><i class="fas fa-map-marker-alt"></i> Address visited</span>:');
                    self.setContentAppend('<br><i>' + response.passportAddress + '</i>');
                    showOcrMap = true;
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            },
            onContentReady: function () {
                if (showOcrMap) {
                    showOcrMap = false;
                    initPopMap(data.passportPosition, data.passportAddress);
                }
            }
        })
    })

    $('.audiolocationImage').click(function () {
        var data;
        $.confirm({
            type: 'green',
            closeIcon: true,
            columnClass: 'medium',

            buttons: {
                confirm: {
                    text: "Ok",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/ClaimsInvestigation/GetInvestigationAudioData?claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    data = response;
                    self.setTitle('<i class="fas fa-portrait"></i> Audio <span class="badge badge-light">uploaded location</span>');
                    self.setContent('<span class="badge badge-light"><i class="fas fa-map-pin"></i> Location visited</span>:');
                    self.setContentAppend('<div id="maps"></div>')
                    self.setContentAppend('<br><div id="pop-map"></div>')
                    self.setContentAppend('</div>')
                    self.setContentAppend('<span class="badge badge-light"><i class="fas fa-map-marker-alt"></i> Address visited</span>:');
                    self.setContentAppend('<br><i>' + response.audioAddress + '</i>');
                    showOcrMap = true;
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            },
            onContentReady: function () {
                if (showOcrMap) {
                    showOcrMap = false;
                    initPopMap(data.audioPosition, data.audioAddress);
                }
            }
        })
    })

    $('.videolocationImage').click(function () {
        var data;
        $.confirm({
            type: 'green',
            closeIcon: true,
            columnClass: 'medium',

            buttons: {
                confirm: {
                    text: "Ok",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/ClaimsInvestigation/GetInvestigationVideoData?claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    data = response;
                    self.setTitle('<i class="fas fa-portrait"></i> Video <span class="badge badge-light">uploaded location</span>');
                    self.setContent('<span class="badge badge-light"><i class="fas fa-map-pin"></i> Location visited</span>:');
                    self.setContentAppend('<div id="maps"></div>')
                    self.setContentAppend('<br><div id="pop-map"></div>')
                    self.setContentAppend('</div>')
                    self.setContentAppend('<span class="badge badge-light"><i class="fas fa-map-marker-alt"></i> Address visited</span>:');
                    self.setContentAppend('<br><i>' + response.videoAddress + '</i>');
                    showOcrMap = true;
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            },
            onContentReady: function () {
                if (showOcrMap) {
                    showOcrMap = false;
                    initPopMap(data.videoPosition, data.videoAddress);
                }
            }
        })
    })

    $('.ocr-Image').click(function () {
        $.confirm({
            type: 'green',
            closeIcon: true,

            buttons: {
                confirm: {
                    text: "Ok",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/ClaimsInvestigation/GetInvestigationPanData?claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    self.setTitle('<i class="fas fa-portrait"></i> Pan card <span class="badge badge-light">Uploaded</span>');
                    self.setContent('<span class="badge badge-light"><i class="fas fa-film"></i> Pan card scanned Image</span>');
                    self.setContentAppend('<br><img id="agentOcrPicture" class="img-fluid investigation-actual-image" src="' + response.ocrData + '" /> ');
                    self.setContentAppend('<br><span class="badge badge-light"><i class="fas fa-info"></i> Image Scan Info</span> ');
                    self.setContentAppend('<br><i>' + response.qrData + '</i>');
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            }
        })
    })

    $('.passport-Image').click(function () {
        $.confirm({
            type: 'green',
            closeIcon: true,

            buttons: {
                confirm: {
                    text: "Ok",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/ClaimsInvestigation/GetInvestigationPassportData?claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    self.setTitle('<i class="fas fa-portrait"></i> Passport <span class="badge badge-light">Uploaded</span>');
                    self.setContent('<span class="badge badge-light"><i class="fas fa-film"></i> Passport scanned Image</span>');
                    self.setContentAppend('<br><img id="agentPassportPicture" class="img-fluid investigation-actual-image" src="' + response.passportImage + '" /> ');
                    self.setContentAppend('<br><span class="badge badge-light"><i class="fas fa-info"></i> Image Scan Info</span> ');
                    self.setContentAppend('<br><i>' + response.passportData + '</i>');
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            }
        })
    })

});

// Function to disable all interactive elements
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

async function initPopMap(_position, title) {
    const { Map } = await google.maps.importLibrary("maps");
    // The location of Uluru
    var position = { lat: -25.344, lng: 131.031 };
    if (_position) {
        position = _position;
    }
    var element = document.getElementById("pop-map");
    // The map, centered at Uluru
    mapz = new Map(element, {
        scaleControl: true,
        zoom: 14,
        center: position,
        mapId: "4504f8b37365c3d0",
        mapTypeId: google.maps.MapTypeId.ROADMAP,
    });

    var marker = new google.maps.Marker({ position: position, map: mapz, title: title })
}
function enableSubmitButton(obj, showDefaultOption = true) {
    var value = obj.value;
    $('#create-pincode').prop('disabled', false);
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