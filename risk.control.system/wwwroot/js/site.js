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


//document.addEventListener("DOMContentLoaded", function () {
//    var ws = new WebSocket('wss://' + window.location.host + '/ws');
//    ws.onopen = function () {
//        ws.send("Hello, server!");
//    };
//    ws.onmessage = function (event) {
//        console.log("Received message: " + event.data);
//    };
//    ws.onerror = function (error) {
//        console.error("WebSocket error: " + error);
//    };
//});

function openForm() {
    document.getElementById("myForm").style.display = "block";
}

function closeForm() {
    document.getElementById("myForm").style.display = "none";
}
function clearAllInputs(event) {
    var allInputs = document.querySelectorAll('input');

    allInputs.forEach(singleInput => singleInput.readOnly ? singleInput.value: singleInput.value = '');
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

async function fetchIpInfo(latlong) {
    try {
        if (latlong) {
            const url = "/api/Notification/GetClientIp?url=" + encodeURIComponent(window.location.pathname) + "&latlong=" + encodeURIComponent(latlong);
            var parser = new UAParser();
            var result = parser.getResult();
            const response = await fetch(url);
            if (!response.ok) {
                const data = {};
                console.error('There has been a problem with your ip fetch operation');
                document.querySelector('#ipAddress .info-data').textContent = 'Not available';
                document.querySelector('#city .info-data').textContent = 'Not available';
            }
            else {
                const data = await response.json();
                document.querySelector('#ipAddress .info-data').textContent = data.ipAddress || 'Not available';
                document.querySelector('#city .info-data').textContent = data.district || 'Not available';
            }
            document.querySelector('#browser .info-data').textContent = result.browser.name.toLowerCase() + '' + result.browser.major || 'Not available';
            document.querySelector('#device .info-data').textContent = getDeviceType() || 'Not available';
            document.querySelector('#os .info-data').textContent = result.os.name.toLowerCase() + '' + result.os.version || 'Not available';
        }

    } catch (error) {
        console.error('There has been a problem with your fetch operation:', error);
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(success, error);
        } else {
            console.error('Geolocation is not supported by this browser.');
        }
    }
}
function success(position) {
    var lat = position.coords.latitude;
    var long = position.coords.longitude;
    var latlong = lat + "," + long; // Store lat and long in the latlong variable
    fetchIpInfo(latlong);
}
function error() {
    console.error('Geolocation request failed or was denied.');
}
$(document).ready(function () {
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

    $('#back').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#back').attr('disabled', 'disabled');
        $('html').css('cursor', 'not-allowed');
        $('a, button').css('cursor', 'not-allowed');
        $('a, button').attr('disabled', 'disabled');
        var section = document.getElementById("section");
        if (section) {
            var nodes = section.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('#back-button').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#back-button').attr('disabled', 'disabled');
        $('html').css('cursor', 'not-allowed');
        $('a, button').css('cursor', 'not-allowed');
        $('a, button').attr('disabled', 'disabled');
        var section = document.getElementById("section");
        if (section) {
            var nodes = section.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
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

    if ($(".selected-case:checked").length) {
        $("#allocate-case").prop('disabled', false);
    }
    else {
        $("#allocate-case").prop('disabled', true);
    }

    // When user checks a radio button, Enable submit button
    $(".selected-case").change(function (e) {
        if ($(this).is(":checked")) {
            $("#allocate-case").prop('disabled', false);
        }
        else {
            $("#allocate-case").prop('disabled', true);
        }
    });

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

    $("#btnDeleteImage").click(function () {
        var id = $(this).attr("data-id");
        $.ajax({
            url: '/User/DeleteImage/' + id,
            type: "POST",
            async: true,
            success: function (data) {
                if (data.succeeded) {
                    $("#delete-image-main").hide();
                    $("#ProfilePictureUrl").val("");
                }
                else {
                    // toastr.error(data.message);
                }
            },
            beforeSend: function () {
                $(this).attr("disabled", true);
                $(this).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Loading...');
            },
            complete: function () {
                $(this).html('Delete Image');
            },
        });
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

function loadState(obj, showDefaultOption = true) {
    var value = obj.value;
    if (value == '') {
        $("#StateId").empty();
        $("#DistrictId").empty();
        $("#PinCodeId").empty();
        $("#PinCodeId").val('');

        $("#StateId").append("<option value=''>--- SELECT ---</option>");
        $("#DistrictId").append("<option value=''>--- SELECT ---</option>");
        $("#PinCodeId").append("<option value=''>--- SELECT ---</option>");
    }
    else {
        $.get("/api/MasterData/GetStatesByCountryId", { countryId: value }, function (data) {
            PopulateStateDropDown("#PinCodeId", "#DistrictId", "#StateId", data, "<option value=''>--- SELECT ---</option>", "<option value=''>--- SELECT ---</option>", "<option value=''>--- SELECT ---</option>", showDefaultOption);
        });
    }

}
function loadDistrict(obj, showDefaultOption = true) {
    var value = obj.value;
    if (value == '') {
        $("#DistrictId").empty();
        $("#PinCodeId").empty();
        $("#PinCodeId").val('');

        $("#DistrictId").append("<option value=''>--- SELECT ---</option>");
        $("#PinCodeId").append("<option value=''>--- SELECT ---</option>");
    }
    else {
        $.get("/api/MasterData/GetDistrictByStateId", { stateId: value }, function (data) {
            PopulateDistrictDropDown("#PinCodeId", "#DistrictId", data, "<option value=''>--- SELECT ---</option>", "<option value=''>--- SELECT ---</option>", showDefaultOption);
        });
    }
}
function loadPinCode(obj, showDefaultOption = true) {
    var value = obj.value;
    if (value == '') {
        $("#PinCodeId").empty();
        $("#PinCodeId").val('');

        $("#PinCodeId").append("<option value=''>--- SELECT ---</option>");
    }
    else {
        $.get("/api/MasterData/GetPinCodesByDistrictId", { districtId: value }, function (data) {
            PopulatePinCodeDropDown("#PinCodeId", data, "<option value=''>--- SELECT ---</option>", showDefaultOption);
        });
    }
}

function enableSubmitButton(obj, showDefaultOption = true) {
    var value = obj.value;
    $('#create-pincode').prop('disabled', false);
}

function loadSubStatus(obj) {
    var value = obj.value;
    $.get("/api/MasterData/GetSubstatusBystatusId", { InvestigationCaseStatusId: value }, function (data) {
        PopulateSubStatus("#InvestigationCaseSubStatusId", data, "<option>--- SELECT ---</option>");
    });
}

function PopulateSubStatus(dropDownId, list, option) {
    $(dropDownId).empty();
    $(dropDownId).append(option)
    $.each(list, function (index, row) {
        $(dropDownId).append("<option value='" + row.investigationServiceTypeId + "'>" + row.code + "</option>")
    });
}
let lobObj;
let investigationServiceObj;

function loadInvestigationServices(obj) {
    var value = obj.value;
    if (value == '') {
        $('#InvestigationServiceTypeId').empty();
        $('#InvestigationServiceTypeId').append("<option value=''>--- SELECT ---</option>");
    }
    else {
        lobObj = value;
        localStorage.setItem('lobId', value);
        $.get("/api/MasterData/GetInvestigationServicesByLineOfBusinessId", { LineOfBusinessId: value }, function (data) {
            PopulateInvestigationServices("#InvestigationServiceTypeId", data, "<option>--- SELECT ---</option>");
        });
    }
}

function setInvestigationServices(obj) {
    localStorage.setItem('serviceId', obj.value);
    investigationServiceObj = obj.value;
}
function PopulateInvestigationServices(dropDownId, list, option) {
    $(dropDownId).empty();
    $(dropDownId).append(option)
    $.each(list, function (index, row) {
        $(dropDownId).append("<option value='" + row.investigationServiceTypeId + "'>" + row.name + "</option>")
    });
}
function PopulateDistrictDropDown(pinCodedropDownId, districtDropdownId, list, pincodeOption, districtOption, showDefaultOption) {
    $(pinCodedropDownId).empty();
    $(pinCodedropDownId).val('');
    $(pinCodedropDownId).append(pincodeOption)

    $(districtDropdownId).empty();
    $(districtDropdownId).append(districtOption)

    $.each(list, function (index, row) {
        $(districtDropdownId).append("<option value='" + row.districtId + "'>" + row.name + "</option>")
    });
}
function PopulatePinCodeDropDown(dropDownId, list, option, showDefaultOption) {
    $(dropDownId).empty();
    $(dropDownId).val('');
    if (showDefaultOption)
        $(dropDownId).append(option)
    if (list && list.length > 0) {
        $.each(list, function (index, row) {
            $(dropDownId).append("<option value='" + row.pinCodeId + "'>" + row.name + " -- " + row.code + "</option>");
            $('#create-pincode').prop('disabled', false);
        });
    }
    else {
        $(dropDownId).append("<option value='-1'>NO - PINCODE - AVAILABLE</option>")
        $('#create-pincode').prop('disabled', true);
    }
}
function PopulateStateDropDown(pinCodedropDownId, districtDropDownId, stateDropDownId, list, stateOption, districtOption, pincodeOption, showDefaultOption) {
    $(stateDropDownId).empty();
    $(districtDropDownId).empty();
    $(pinCodedropDownId).empty();
    $(pinCodedropDownId).val('');

    $(stateDropDownId).append(stateOption);
    $(districtDropDownId).append(districtOption);
    $(pinCodedropDownId).append(pincodeOption);

    $.each(list, function (index, row) {
        $(stateDropDownId).append("<option value='" + row.stateId + "'>" + row.name + "</option>")
    });
}
function toggleChecked(status) {
    $("#checkboxes input").each(function () {
        // Set the checked status of each to match the
        // checked status of the check all checkbox:
        $(this).prop("checked", status);
    });
}
function loadRemainingPinCode(obj, showDefaultOption = true, caseId) {
    var value = obj.value;
    $.get("/api/MasterData/GetPincodesByDistrictIdWithoutPreviousSelected", { districtId: value, caseId: caseId }, function (data) {
        PopulatePinCodeDropDown("#PinCodeId", data, "<option>--SELECT PINCODE--</option>", showDefaultOption);
    });
}
function loadRemainingServicePinCode(obj, showDefaultOption = true, vendorId, lineId) {
    var value = obj.value;

    var lobId = localStorage.getItem('lobId');

    var serviceId = localStorage.getItem('serviceId');

    $.get("/api/MasterData/GetPincodesByDistrictIdWithoutPreviousSelectedService", { districtId: value, vendorId: vendorId, lobId: lobId, serviceId: serviceId }, function (data) {
        PopulatePinCodeDropDown("#PinCodeId", data, "<option>--SELECT PINCODE--</option>", showDefaultOption);
    });
}

function DisableBackButton() {
    window.history.forward()
}
DisableBackButton();
window.onload = DisableBackButton;
window.onpageshow = function (evt) { if (evt.persisted) DisableBackButton() }
window.onunload = function () { void (0) }
if (navigator.geolocation) {
    navigator.geolocation.getCurrentPosition(success, error);
} else {
    console.error('Geolocation is not supported by this browser.');
}

