$(document).ready(function () {

    // Load the customer details
     $('#customerGoogleMap').click(function () {
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
                    url: '/api/CaseInvestigationDetails/GetCustomerMap?id=' + $('#customerDetailId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    data = response;
                    self.setTitle('<i class="fas fa-mobile-alt"></i> <b>Customer Address Location</b>');
                    self.setContent('<b><span class="badge badge-light"><i class="fas fa-map-pin"></i> Map Location</span></b>:');
                    self.setContentAppend('<span class="badge badge-light"><i class="fas fa-map-marker-alt"></i> Address</span>:');
                    self.setContentAppend('<br><img class="img-fluid investigation-actual-image" src="' + response.profileMap + '" /> ');
                    self.setContentAppend('<span class="badge badge-light"><i class="fas fa-map-marker-alt"></i> Address</span>:');
                    self.setContentAppend('<br><i>' + response.address + '</i>');
                    self.setContentAppend('<br><span class="badge badge-light"><i class="fas fa-info"></i> Location Info</span> :');
                    self.setContentAppend('<br><i>' + response.weatherData + '</i>');
                    showCustomerMap = true;
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            },
            //onContentReady: function () {
            //    if (showCustomerMap) {
            //        showCustomerMap = false;
            //        initPopMap(data.position, data.address);
            //    }
            //}
        })
    });

    // Load the bdeneficiary details
     $('#beneficiaryGoogleMap').click(function () {
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
                    url: '/api/CaseInvestigationDetails/GetBeneficiaryMap?id=' + $('#beneficiaryId').val() + '&claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    data = response;
                    self.setTitle('<i class="fas fa-mobile-alt"></i> <b>Beneficiary Address Location</b>');
                    self.setContent('<b><span class="badge badge-light"><i class="fas fa-map-pin"></i> Map Location</span></b>:');
                    self.setContentAppend('<br><img class="img-fluid investigation-actual-image" src="' + response.profileMap + '" /> ');
                    self.setContentAppend('<span class="badge badge-light"><i class="fas fa-map-marker-alt"></i> Address</span>:');
                    self.setContentAppend('<br><i>' + response.address + '</i>');
                    self.setContentAppend('<br><span class="badge badge-light"><i class="fas fa-info"></i> Location Info</span> :');
                    self.setContentAppend('<br><i>' + response.weatherData + '</i>');
                    showBeneficiaryMap = true;
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            },
            //onContentReady: function () {
            //    if (showBeneficiaryMap) {
            //        showBeneficiaryMap = false;
            //        initPopMap(data.position, data.address);
            //    }
            //}
        })
    })


     $('#policy-detail').click(function () {
        $.confirm({
            title: "Policy details",
            closeIcon: true,
            type: 'blue',
            buttons: {
                confirm: {
                    text: "Close",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    'type': 'GET',
                    'url': '/api/CaseInvestigationDetails/GetPolicyDetail?id=' + $('#policyDetailId').val(),
                    'dataType': 'json',
                    method: 'get'
                }).done(function (response) {
                    self.setTitle('<i class="far fa-file-powerpoint"></i> Policy details ');
                    self.setContent('<article>');
                    self.setContent('<div class="bb-blog-inner">');

                    self.setContentAppend('<div class="card card-solid">');
                    self.setContentAppend('<header class="bb-blog-header">');
                    self.setContentAppend('<h5 class="bb-blog-title" itemprop="name">Policy #: ' + response.contractNumber);
                    self.setContentAppend('</header>');
                    self.setContentAppend('<div class="card-body pb-0">');
                    self.setContentAppend('<div class="row">');
                    self.setContentAppend('<b> Claim Type: </b>' + response.claimType);
                    self.setContentAppend('<br><p class="fa-li">');
                    self.setContentAppend('<b><i class="fa fa-money"></i> Assured Amount</b>:  ' + response.sumAssuredValue);
                    self.setContentAppend('</p');
                    self.setContentAppend('<br><p class="fa-li">');
                    self.setContentAppend('<b><i class="far fa-clock"></i> Policy Issue Date</b>:  ' + response.contractIssueDate);
                    self.setContentAppend('</p');
                    self.setContentAppend('<br><p class="fa-li">');
                    self.setContentAppend('<b><i class="fas fa-clock"></i>  Incident Date</b>: ' + response.dateOfIncident);
                    self.setContentAppend('</p');
                    self.setContentAppend('<br><p class="fa-li">');
                    self.setContentAppend('<b><i class="fas fa-tools"></i> Service Type</b>:  ' + response.investigationServiceType);
                    self.setContentAppend('</p');
                    self.setContentAppend('<br><p class="fa-li">');
                    self.setContentAppend('<b> <i class="fas fa-bolt"></i> Reason to verify</b>: ' + response.caseEnabler);
                    self.setContentAppend('</p');
                    self.setContentAppend('<br><p class="fa-li">');
                    self.setContentAppend('<b><i class="far fa-check-circle"></i> Cause of Incidence</b>:  ' + response.causeOfLoss);
                    self.setContentAppend('</p');
                    self.setContentAppend('<br><p class="fa-li">');
                    self.setContentAppend('<b><i class="fas fa-money-check-alt"></i> Budget Centre</b>:  ' + response.costCentre);
                    self.setContentAppend('</p');
                    self.setContentAppend('<br><b><i class="far fa-id-badge"></i> Policy Document</b>:');
                    self.setContentAppend('<br><img id="agentLocationPicture" class="img-fluid investigation-actual-image" src="' + response.document + '" /> ');
                    self.setContentAppend('</p');
                    self.setContentAppend('</div>');
                    self.setContentAppend('</div>');
                    self.setContentAppend('</div>');
                    self.setContentAppend('</div>');
                    self.setContentAppend('</article>');
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            }
        })
    });

    $('#customer-detail').click(function () {
        $.confirm({

            title: "Customer detail",
            icon: 'fa fa-user-plus',
            closeIcon: true,

            type: 'orange',
            buttons: {
                confirm: {
                    text: "Close",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/CaseInvestigationDetails/GetCustomerDetail?id=' + $('#customerDetailId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    self.setContent('<hr>');
                    self.setContentAppend('<header>');
                    self.setContentAppend('<b><i class="fa fa-user-plus"></i> Customer Name</b>: ' + response.customerName);
                    self.setContentAppend('</header>');
                    self.setContentAppend('<br><b><i class="far fa-clock"></i> Date of birth</b> : ' + response.dateOfBirth);
                    self.setContentAppend('<br><b><i class="fas fa-tools"></i> Occupation</b> : ' + response.occupation);
                    self.setContentAppend('<br><b><i class="fa fa-money"></i> Annual Income</b> : ' + response.income);
                    self.setContentAppend('<br><b><i class="fas fa-user-graduate"></i> Education</b> : ' + response.education);
                    self.setContentAppend('<br><b><i class="fas fa-home"></i> Address</b> : ' + response.address);
                    self.setContentAppend('<br><b><i class="fas fa-lg fa-phone"></i> Phone</b> : ' + response.contactNumber);
                    self.setContentAppend('<br><b><i class="far fa-id-badge"></i> Customer Image</b>:');
                    self.setContentAppend('<br><img id="agentLocationPicture" class="img-fluid investigation-actual-image" src="' + response.customer + '" />');
                    self.setTitle('Customer details');
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            }
        })
    })

    $('#beneficiary-detail').click(function () {
        $.confirm({

            title: "Beneficiary details",
            icon: 'fas fa-user-tie',
            closeIcon: true,

            type: 'green',
            buttons: {
                confirm: {
                    text: "Close",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/CaseInvestigationDetails/GetBeneficiaryDetail?id=' + $('#beneficiaryId').val() + '&claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    self.setContent('<header>');
                    self.setContentAppend('<hr>');
                    self.setContentAppend('<b><i class="fas fa-user-tie"></i> Beneficiary Name</b>: ' + response.beneficiaryName);
                    self.setContentAppend('</header>');
                    self.setContentAppend('<br><b><i class="fas fa-user-tag"></i>  Relation</b> : ' + response.beneficiaryRelation);
                    self.setContentAppend('<br><b><i class="fas fa-lg fa-phone"></i> Phone</b>: ' + response.contactNumber);
                    self.setContentAppend('<br><b><i class="fa fa-money"></i> Annual Income</b>: ' + response.income);
                    self.setContentAppend('<br><b><i class="fas fa-home"></i> Address</b>: ' + response.address);
                    self.setContentAppend('<br><b><i class="far fa-id-badge"></i> Beneficiary Image</b>:');
                    self.setContentAppend('<br><img id="agentLocationPicture" class="img-fluid investigation-actual-image" src="' + response.beneficiary + '" /> ');
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            }
        })
    })

    $('#notesDetail').click(function () {
        $.confirm({
            title: 'Policy Note!!!',
            closeIcon: true,
            type: 'green',
            icon: 'far fa-file-powerpoint',
            buttons: {
                confirm: {
                    text: "Close",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                const date = new Date();
                const day = String(date.getDate()).padStart(2, '0');
                const month = String(date.getMonth() + 1).padStart(2, '0'); // Months are zero-based
                const year = date.getFullYear();
                const formattedDate = `${day}-${month}-${year}`;

                const hours = String(date.getHours()).padStart(2, '0');
                const minutes = String(date.getMinutes()).padStart(2, '0');
                const seconds = String(date.getSeconds()).padStart(2, '0');
                const formattedTime = `${hours}:${minutes}:${seconds}`;

                return $.ajax({
                    url: '/api/CaseInvestigationDetails/GetPolicyNotes?claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    self.setContent('<header>');
                    self.setContentAppend('</header>');
                    $.each(response.notes, function (index, note) {
                        self.setContentAppend('<hr>');
                        self.setContentAppend('<b><i class="fas fa-clock"></i> Notes added date</b>: ' + formattedDate);
                        self.setContentAppend('<br><b><i class="fas fa-clock"></i> Notes added time</b>: ' + formattedTime);
                        self.setContentAppend('<br><b><i class="fas fa-user-tag"></i>  Sender</b> : ' + note.sender);
                        self.setContentAppend('<br><b><i class="far fa-id-badge"></i> Note</b>: ' + note.comment);
                        self.setContentAppend('<hr>');
                    })
                }).fail(function () {
                    self.setContent('Something went wrong.');
                }).always(function () {
                    
                });
            }
        })
    })

    $('#policy-comments').click(function () {
        var claimId = $('#claimId').val();
        const imgElement = document.getElementById("notesDetail-disabled");
        $.confirm({
            title: 'Policy Note!!!',
            closeIcon: true,
            type: 'green',
            icon: 'far fa-file-powerpoint',
            content: '' +
                '<form method="post" action="Confirm/AddNotes?claimId="' + claimId + ' class="formName">' +
                '<div class="form-group">' +
                '<hr>' +
                '<label>Enter note on Policy</label>' +
                '<input type="text" placeholder="Enter note" class="name form-control remarks" required />' +
                '</div>' +
                '</form>',
            buttons: {
                formSubmit: {
                    text: 'Add Note',
                    btnClass: 'btn-green',
                    action: function () {
                        var name = this.$content.find('.name').val();
                        if (!name) {
                            $.alert({
                                title: 'Provide Policy note !!!',
                                closeIcon: true,
                                type: 'red',
                                icon: 'far fa-file-powerpoint',
                                content: 'Provide Policy note !!!'
                            });
                            return false;
                        }
                        else {
                            
                            return $.ajax({
                                url: '/Confirm/AddNotes?claimId=' + $('#claimId').val() + '&name=' + name,
                                method: 'get'
                            }).done(function (response) {
                                $.alert({
                                    title: 'Policy notes added!',
                                    closeIcon: true,
                                    type: 'green',
                                    icon: 'far fa-comments',
                                    content: 'Status: ' + response.message,
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
                                if (imgElement) {
                                    imgElement.title = "Display notes"
                                    imgElement.id = "notesDetail";
                                    imgElement.src = "/img/blank-document.png";
                                    imgElement.addEventListener("click", function () {
                                        $.confirm({
                                            title: 'Policy Note!!!',
                                            closeIcon: true,
                                            type: 'green',
                                            icon: 'far fa-file-powerpoint',
                                            buttons: {
                                                confirm: {
                                                    text: "Close",
                                                    btnClass: 'btn-secondary',
                                                    action: function () {
                                                        askConfirmation = false;
                                                    }
                                                }
                                            },
                                            content: function () {
                                                var self = this;
                                                const date = new Date();
                                                const day = String(date.getDate()).padStart(2, '0');
                                                const month = String(date.getMonth() + 1).padStart(2, '0'); // Months are zero-based
                                                const year = date.getFullYear();
                                                const formattedDate = `${day}-${month}-${year}`;

                                                const hours = String(date.getHours()).padStart(2, '0');
                                                const minutes = String(date.getMinutes()).padStart(2, '0');
                                                const seconds = String(date.getSeconds()).padStart(2, '0');
                                                const formattedTime = `${hours}:${minutes}:${seconds}`;

                                                return $.ajax({
                                                    url: '/api/CaseInvestigation/GetPolicyNotes?claimId=' + $('#claimId').val(),
                                                    dataType: 'json',
                                                    method: 'get'
                                                }).done(function (response) {
                                                    self.setContent('<header>');
                                                    self.setContentAppend('</header>');
                                                    $.each(response.notes, function (index, note) {
                                                        self.setContentAppend('<hr>');
                                                        self.setContentAppend('<b><i class="fas fa-clock"></i> Notes added date</b>: ' + formattedDate);
                                                        self.setContentAppend('<br><b><i class="fas fa-clock"></i> Notes added time</b>: ' + formattedTime);
                                                        self.setContentAppend('<br><b><i class="fas fa-user-tag"></i>  Sender</b> : ' + note.sender);
                                                        self.setContentAppend('<br><b><i class="far fa-id-badge"></i> Note</b>: ' + note.comment);
                                                        self.setContentAppend('<hr>');
                                                    })
                                                }).fail(function () {
                                                    self.setContent('Something went wrong.');
                                                }).always(function () {

                                                });
                                            }
                                        });
                                    });
                                }
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
    var ready = false;
    $('#customer-comments').click(function (e) {
        var claimId = $('#claimId').val();
        $.confirm({
            title: 'SMS Customer !!!',
            closeIcon: true,
            type: 'green',
            icon: 'fa fa-user-plus',
            content: '' +
                '<form method="post" action="Confirm/Sms2Customer?claimId="' + claimId + ' class="formName">' +
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
                                url: '/Confirm/Sms2Customer?claimId=' + claimId + '&name=' + name,
                                method: 'get'
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
        $.confirm({
            title: 'SMS Beneficiary !!!',
            icon: 'fas fa-user-tie',
            closeIcon: true,
            type: 'green',
            content: '' +
                '<form method="post" action="Confirm/Sms2Beneficiary?claimId="' + claimId + ' class="formName">' +
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
                                url: '/Confirm/Sms2Beneficiary?claimId=' + claimId + '&name=' + name,
                                method: 'get'
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