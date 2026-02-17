$(document).ready(function () {
    $('#customerGoogleMap').on('click', function () {
        const customerId = $('#customerDetailId').val();

        if (!customerId) {
            $.alert('Customer details not found.');
            return;
        }

        $.confirm({
            type: 'green',
            closeIcon: true,
            columnClass: 'medium',
            title: '<i class="fas fa-map-marked-alt"></i> Customer Address Location',
            content: function () {
                const self = this;

                return $.ajax({
                    url: '/api/CaseInvestigationDetails/GetCustomerMap',
                    method: 'GET',
                    dataType: 'json',
                    data: { id: customerId }
                })
                    .done(function (res) {
                        self.setContent(renderMapHtml(res));
                    })
                    .fail(function (xhr) {
                        if (xhr.status === 401 || xhr.status === 403) {
                            handleSessionExpired();
                        } else {
                            self.setContent('<span class="text-danger">Unable to load map details.</span>');
                        }
                    });
            },
            buttons: {
                ok: {
                    text: 'Ok',
                    btnClass: 'btn-secondary'
                }
            }
        });
    });

    $('#beneficiaryGoogleMap').on('click', function () {
        const beneficiaryId = $('#beneficiaryId').val();

        if (!beneficiaryId) {
            $.alert('Beneficiary details not found.');
            return;
        }

        $.confirm({
            type: 'green',
            closeIcon: true,
            columnClass: 'medium',
            title: '<i class="fas fa-map-marked-alt"></i> Beneficiary Address Location',

            content: function () {
                const self = this;

                return $.ajax({
                    url: '/api/CaseInvestigationDetails/GetBeneficiaryMap',
                    method: 'GET',
                    dataType: 'json',
                    data: {
                        id: beneficiaryId
                    }
                })
                    .done(function (res) {
                        self.setContent(renderMapHtml(res));
                    })
                    .fail(function (xhr) {
                        if (xhr.status === 401 || xhr.status === 403) {
                            handleSessionExpired();   // from idle/session logic
                        } else {
                            self.setContent('<span class="text-danger">Unable to load beneficiary map details.</span>');
                        }
                    });
            },

            buttons: {
                ok: {
                    text: 'Ok',
                    btnClass: 'btn-secondary'
                }
            }
        });
    });

    $('#policy-detail').on('click', function () {
        const policyId = $('#policyDetailId').val();

        if (!policyId) {
            $.alert('Policy details not found.');
            return;
        }

        $.confirm({
            title: '<i class="far fa-file-alt"></i> Policy Details',
            closeIcon: true,
            type: 'blue',
            columnClass: 'large',

            content: function () {
                const self = this;

                return $.ajax({
                    url: '/api/CaseInvestigationDetails/GetPolicyDetail',
                    method: 'GET',
                    dataType: 'json',
                    data: { id: policyId }
                })
                    .done(function (res) {
                        self.setContent(renderPolicyDetailHtml(res, policyId));
                    })
                    .fail(function (xhr) {
                        if (xhr.status === 401 || xhr.status === 403) {
                            handleSessionExpired();
                        } else {
                            self.setContent('<span class="text-danger">Unable to load policy details.</span>');
                        }
                    });
            },

            buttons: {
                close: {
                    text: 'Close',
                    btnClass: 'btn-secondary'
                }
            }
        });
    });

    $('#customer-detail').on('click', function () {
        const customerId = $('#customerDetailId').val();

        if (!customerId) {
            $.alert('Customer details not found.');
            return;
        }

        openDetailPopup({
            title: '<i class="fa fa-user"></i> Customer Details',
            type: 'orange',
            url: '/api/CaseInvestigationDetails/GetCustomerDetail',
            data: { id: customerId },
            render: (res) => renderCustomerDetailHtml(res, customerId)
        });
    });

    $('#beneficiary-detail').on('click', function () {
        const beneficiaryId = $('#beneficiaryId').val();

        if (!beneficiaryId) {
            $.alert('Beneficiary details not found.');
            return;
        }

        openDetailPopup({
            title: '<i class="fas fa-user-tie"></i> Beneficiary Details',
            type: 'green',
            url: '/api/CaseInvestigationDetails/GetBeneficiaryDetail',
            data: { id: beneficiaryId },
            render: (res) => renderBeneficiaryDetailHtml(res, beneficiaryId)
        });
    });

    $('#notesDetail').on('click', function () {
        const claimId = $('#claimId').val();

        if (!claimId) {
            $.alert('Claim details not found.');
            return;
        }

        openDetailPopup({
            title: '<i class="far fa-file-alt"></i> Policy Notes',
            type: 'green',
            columnClass: 'large',
            url: '/api/CaseInvestigationDetails/GetPolicyNotes',
            data: { claimId: claimId },
            render: renderPolicyNotesHtml
        });
    });

    $('#policy-comments').click(function () {
        var claimId = $('#claimId').val();

        $.confirm({
            title: 'Policy Note!!!',
            closeIcon: true,
            type: 'green',
            icon: 'far fa-file-powerpoint',
            content: `
            <form class="formName">
                <div class="form-group">
                    <hr>
                    <label>Enter note on Policy</label>
                    <input type="text" placeholder="Enter note" class="name form-control remarks" required />
                </div>
            </form>`,
            buttons: {
                formSubmit: {
                    text: 'Add Note',
                    btnClass: 'btn-green',
                    action: function () {
                        var noteText = this.$content.find('.name').val();
                        if (!noteText) {
                            $.alert({ title: 'Error', type: 'red', content: 'Please enter a note.' });
                            return false;
                        }

                        return $.ajax({
                            url: '/Confirm/AddNotes',
                            method: 'POST',
                            data: {
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                caseId: claimId,
                                message: noteText
                            }
                        }).done(function (response) {
                            $.alert({
                                title: 'Policy notes added!',
                                content: 'Status: ' + response.message,
                                type: 'green'
                            });
                            if (response.newCount !== undefined) {
                                updateNotesUI(response.newCount);
                            }
                        }).fail(function () {
                            $.alert('Failed to save note.');
                        });
                    }
                },
                cancel: function () { /* Close */ },
            },
            onContentReady: function () {
                var jc = this;
                var $input = jc.$content.find('.name.form-control.remarks');

                // Use a tiny timeout to ensure the modal animation is finished
                // and the DOM is fully interactive
                setTimeout(function () {
                    $input.focus();
                }, 100);
                this.$content.find('form').on('submit', (e) => {
                    e.preventDefault();
                    jc.$$formSubmit.trigger('click');
                });
            }
        });
    });

    var ready = false;
    $('#customer-comments').click(function (e) {
        var claimId = $('#claimId').val();
        $.confirm({
            title: 'SMS Customer !!!',
            closeIcon: true,
            type: 'green',
            icon: 'fa fa-user-plus',
            content: '' +
                '<form class="formName">' +
                '<div class="form-group">' +
                '<hr>' +
                '<label>Enter message</label>' +
                '<input type="text" placeholder="Enter message" class="name form-control remarks" required />' +
                '</div>' +
                '</form>',
            buttons: {
                formSubmit: {
                    text: 'Send SMS',
                    btnClass: 'btn-green',
                    action: function (e) {
                        var name = this.$content.find('.name').val();
                        if (!name) {
                            $.alert({
                                title: 'Enter message !!!',
                                closeIcon: true,
                                type: 'red',
                                icon: 'fa fa-user-plus',
                                content: 'Please enter message'
                            });
                            var input = this.$content.find('.name.form-control.remarks');
                            input.focus();
                            return false;
                        }
                        else {
                            return $.ajax({
                                url: '/Confirm/Sms2Customer',
                                method: 'POST',
                                data: {
                                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                    caseId: claimId,
                                    message: name
                                }
                            }).done(function (response) {
                                $.alert({
                                    title: 'Message Status!',
                                    closeIcon: true,
                                    type: 'green',
                                    icon: 'far fa-comments',
                                    content: 'Status: ' + response.message,
                                    autoClose: 'ok|2000',
                                    buttons: {
                                        ok: {
                                            text: 'Close',
                                        }
                                    }
                                });
                            }).fail(function (response) {
                                $.alert({
                                    title: 'Message Status!',
                                    content: 'Status: failed',
                                });
                            }).always(function () {
                            });
                        }
                    }
                },
                cancel: function () {
                    //close
                },
            },
            onContentReady: function () {
                // bind to events
                var jc = this;
                var input = this.$content.find('.name.form-control.remarks');
                input.focus();
                this.$content.find('form').on('submit', function (e) {
                    // if the user submits the form by pressing enter in the field.
                    e.preventDefault();
                    jc.$$formSubmit.trigger('click'); // reference the button and click it

                    //var form = $('#cust-sms');
                    //form.submit();
                });
            }
        });
    })

    $('#beneficiary-comments').click(function () {
        var claimId = $('#claimId').val();
        const token = $('input[name="__RequestVerificationToken"]').val();
        $.confirm({
            title: 'SMS Beneficiary !!!',
            icon: 'fas fa-user-tie',
            closeIcon: true,
            type: 'green',
            content: '' +
                '<form class="formName">' +
                '<div class="form-group">' +
                '<hr>' +
                '<label>Enter message</label>' +
                '<input type="text" placeholder="Enter message" class="name form-control remarks" required />' +
                '</div>' +
                '</form>',
            buttons: {
                formSubmit: {
                    text: 'Send SMS',
                    btnClass: 'btn-green',
                    action: function () {
                        var name = this.$content.find('.name').val();
                        if (!name) {
                            $.alert({
                                title: 'Enter message !!!',
                                closeIcon: true,
                                type: 'red',
                                icon: 'fas fa-user-tie',
                                content: 'Please enter message'
                            });
                            return false;
                        }
                        else {
                            return $.ajax({
                                url: '/Confirm/Sms2Beneficiary',
                                method: 'POST',
                                data: {
                                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                    caseId: claimId,
                                    message: name
                                }
                            }).done(function (response) {
                                $.alert({
                                    title: 'Message Status!',
                                    closeIcon: true,
                                    type: 'green',
                                    icon: 'fa fa-user-tie',
                                    content: 'Status: ' + response.message,
                                    autoClose: 'ok|2000',
                                    buttons: {
                                        ok: {
                                            text: 'Close',
                                        }
                                    }
                                });
                            }).fail(function (response) {
                                $.alert({
                                    title: 'Message Status!',
                                    content: 'Status: failed',
                                });
                            }).always(function () {
                            });
                        }
                    }
                },
                cancel: function () {
                    //close
                },
            },
            onContentReady: function () {
                // bind to events
                var jc = this;
                var input = this.$content.find('.name.form-control.remarks');
                input.focus();
                this.$content.find('form').on('submit', function (e) {
                    // if the user submits the form by pressing enter in the field.
                    e.preventDefault();
                    jc.$$formSubmit.trigger('click'); // reference the button and click it
                });
            }
        });
    })

    $('a#assign-manual').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('a#assign-manual').html("<i class='fas fa-sync fa-spin'></i> Assign<sub>manual</sub> ");

        // Disable all buttons, submit inputs, and anchors
        $('button, input[type="submit"], a').prop('disabled', true);

        // Add a class to visually indicate disabled state for anchors
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });
        $('a').attr('disabled', 'disabled');
        $('button').attr('disabled', 'disabled');
        $('html button').css('pointer-events', 'none');
        $('html a').css({ 'pointer-events': 'none' }, { 'cursor': 'none' });
        $('.text').css({ 'pointer-events': 'none' }, { 'cursor': 'none' });

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });
});
function updateNotesUI(newCount) {
    // Select both possible IDs to ensure we find the element
    const $badge = $('#notesBadge');
    const $img = $('#notesDetail');

    // Update the number
    $badge.text(newCount);

    if (newCount > 0) {
        // Change Badge Color
        $badge.removeClass('bg-secondary').addClass('bg-danger');

        // Update Image State
        $img.addClass('notes-active')
            .attr('title', 'Display notes')
            .attr('data-bs-original-title', 'Display notes'); // For Bootstrap tooltips
    }
}

// Logic to show existing notes (Extracted from your 'always' block)
function showNotesModal() {
    $.confirm({
        title: 'Policy Notes',
        columnClass: 'medium',
        closeIcon: true,
        type: 'blue',
        icon: 'fas fa-list',
        buttons: { close: { text: "Close", btnClass: 'btn-secondary' } },
        content: function () {
            var self = this;
            return $.ajax({
                url: '/api/CaseInvestigationDetails/GetPolicyNotes?claimId=' + $('#claimId').val(),
                method: 'GET'
            }).done(function (response) {
                let html = '<div class="notes-wrapper">';
                $.each(response.notes, function (index, note) {
                    // Use your detailRow style or manual formatting
                    html += `
                        <div class="note-item mb-3">
                            <small class="text-muted"><i class="fas fa-clock"></i> ${note.created || 'N/A'}</small><br>
                            <strong><i class="fas fa-user"></i> ${note.senderEmail}:</strong>
                            <p class="border-left pl-2">${note.comment}</p>
                            <hr>
                        </div>`;
                });
                html += '</div>';
                self.setContent(html || 'No notes found.');
            }).fail(() => self.setContent('Failed to load notes.'));
        }
    });
}
function renderPolicyNotesHtml(data) {
    if (!data || !data.notes || data.notes.length === 0) {
        return `<div class="text-muted text-center">No notes available.</div>`;
    }

    return `
        <article>
            ${data.notes.map(renderSingleNote).join('')}
        </article>
    `;
}

function renderSingleNote(note) {
    const created = note.created
        ? formatDateTime(note.created)
        : { date: '-', time: '-' };

    return `
        <div class="card card-outline card-success mb-3">
            <div class="card-body">

                ${detailRow('<i class="fas fa-calendar-alt"></i>', 'Date', created.date)}
                ${detailRow('<i class="fas fa-clock"></i>', 'Time', created.time)}
                ${detailRow('<i class="fas fa-user-tag"></i>', 'Sender', note.senderEmail)}
                ${detailRow('<i class="far fa-sticky-note"></i>', 'Note', note.comment)}

            </div>
        </div>
    `;
}

function formatDateTime(dateValue) {
    const date = new Date(dateValue);

    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();

    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const seconds = String(date.getSeconds()).padStart(2, '0');

    return {
        date: `${day}-${month}-${year}`,
        time: `${hours}:${minutes}:${seconds}`
    };
}

function renderBeneficiaryDetailHtml(data, beneficiaryId) {
    const beneficiaryImageUrl = `/Document/GetBeneficiaryDocument/${beneficiaryId}`;

    return `
        <article>
            <div class="card card-solid">
                <div class="card-body">

                    ${detailRow('<i class="fas fa-user-tie"></i>', 'Beneficiary Name', data.beneficiaryName)}
                    ${detailRow('<i class="fas fa-user-tag"></i>', 'Relation', data.beneficiaryRelation)}
                    ${detailRow('<i class="fas fa-phone"></i>', 'Phone', data.phoneNumber)}
                    ${detailRow('<i class="fa fa-money"></i>', 'Annual Income', data.income)}
                    ${detailRow('<i class="fas fa-home"></i>', 'Address', data.address)}

                    <hr />

                    <div class="mb-2">
                        <strong><i class="far fa-id-badge"></i> Beneficiary Image</strong>
                    </div>

                    <img class="img-fluid w-50 rounded border"
                         src="${beneficiaryImageUrl}"
                         alt="Beneficiary Image" />
                </div>
            </div>
        </article>
    `;
}

function openDetailPopup(options) {
    $.confirm({
        title: options.title,
        type: options.type || 'blue',
        closeIcon: true,
        columnClass: options.columnClass || 'medium',

        content: function () {
            const self = this;

            return $.ajax({
                url: options.url,
                method: 'GET',
                dataType: 'json',
                data: options.data
            })
                .done(res => self.setContent(options.render(res)))
                .fail(xhr => {
                    if (xhr.status === 401 || xhr.status === 403) {
                        handleSessionExpired();
                    } else {
                        self.setContent('<span class="text-danger">Unable to load details.</span>');
                    }
                });
        },

        buttons: {
            close: {
                text: 'Close',
                btnClass: 'btn-secondary'
            }
        }
    });
}

function renderCustomerDetailHtml(data, customerId) {
    const customerImageUrl = `/Document/GetCustomerDocument/${customerId}`;

    return `
        <article>
        <div class="bb-blog-inner">
            <div class="card card-solid">
                <div class="card-body">

                    ${detailRow('<i class="fa fa-user-plus"></i>', 'Customer Name', data.customerName)}
                    ${detailRow('<i class="far fa-clock"></i>', 'Date of Birth', data.dateOfBirth)}
                    ${detailRow('<i class="fas fa-tools"></i>', 'Occupation', data.occupation)}
                    ${detailRow('<i class="fa fa-money"></i>', 'Annual Income', data.income)}
                    ${detailRow('<i class="fas fa-user-graduate"></i>', 'Education', data.education)}
                    ${detailRow('<i class="fas fa-home"></i>', 'Address', data.address)}
                    ${detailRow('<i class="fas fa-phone"></i>', 'Phone', data.phoneNumber)}

                    <hr />

                    <div class="mb-2">
                        <strong><i class="far fa-id-badge"></i> Customer Image</strong>
                    </div>

                    <img class="img-fluid w-50 rounded border"
                         src="${customerImageUrl}"
                         alt="Customer Image" />
                </div>
            </div>
            </div>
        </article>
    `;
}

function detailRow(icon, label, value) {
    return `
        <div class="mb-2">
            <strong>${icon} ${label}:</strong>
            <span class="ml-1">${value || '-'}</span>
        </div>
    `;
}

function renderPolicyDetailHtml(data, policyId) {
    const policyDocUrl = `/Document/GetPolicyDocument/${policyId}`;

    return `
    <article>
        <div class="bb-blog-inner">
        <div class="card card-solid">
            <header class="card-header">
                <h5 class="mb-0">
                    <i class="far fa-id-card"></i>
                    Policy #: ${data.contractNumber}
                </h5>
            </header>

            <div class="card-body">
                ${renderPolicyRow('<i class="fas fa-clipboard-list"></i>', 'Case Type', data.claimType)}
                ${renderPolicyRow('<i class="fa fa-money"></i>', 'Assured Amount', data.sumAssuredValue)}
                ${renderPolicyRow('<i class="far fa-clock"></i>', 'Policy Issue Date', data.contractIssueDate)}
                ${renderPolicyRow('<i class="fas fa-clock"></i>', 'Incident Date', data.dateOfIncident)}
                ${renderPolicyRow('<i class="fas fa-tools"></i>', 'Service Type', data.investigationServiceType)}
                ${renderPolicyRow('<i class="fas fa-bolt"></i>', 'Reason to Verify', data.caseEnabler)}
                ${renderPolicyRow('<i class="far fa-check-circle"></i>', 'Cause of Incidence', data.causeOfLoss)}
                ${renderPolicyRow('<i class="fas fa-building"></i>', 'Budget Centre', data.costCentre)}

                <hr />

                <div class="mb-2">
                    <strong><i class="far fa-id-badge"></i> Case Document</strong>
                </div>

                <img id="agentLocationPicture" class="img-fluid w-50 rounded border"
                     src="${policyDocUrl}"
                     alt="Policy Document" />
            </div>
        </div>
        </div>
    </article>
    `;
}

function renderPolicyRow(icon, label, value) {
    return `
        <div class="mb-2">
            <strong>${icon} ${label}:</strong>
            <span class="ml-1">${value || '-'}</span>
        </div>
    `;
}

function renderMapHtml(data) {
    return `
        <div class="mb-2">
            <span class="badge badge-light">
                <i class="fas fa-map-pin"></i> Map Location
            </span>
        </div>

        <img class="img-fluid investigation-actual-image mb-2" src="${data.profileMap}" />

        <div class="mb-2">
            <span class="badge badge-light">
                <i class="fas fa-map-marker-alt"></i> Address
            </span><br/>
            <i>${data.address}</i>
        </div>

        <div>
            <span class="badge badge-light">
                <i class="fas fa-info"></i> Location Info
            </span><br/>
            <i>${data.weatherData}</i>
        </div>
    `;
}