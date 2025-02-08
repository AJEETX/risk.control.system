$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Update Password",
            content: "Remeber the new password.",
            icon: 'fa fa-key',

            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Update",
                    btnClass: 'btn-warning',
                    action: function () {
                        askConfirmation = false;
                        $("body").addClass("submit-progress-password-bg");

                        // Update UI with a short delay to show spinner
                        setTimeout(function () {
                            $(".submit-progress-password").removeClass("hidden");
                        }, 1);
                        // Disable all buttons, submit inputs, and anchors
                        $('button, input[type="submit"], a').prop('disabled', true);

                        // Add a class to visually indicate disabled state for anchors
                        $('a').addClass('disabled-anchor').on('click', function (e) {
                            e.preventDefault(); // Prevent default action for anchor clicks
                        });
                        $('#updatebutton').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Update Password");
                        form.submit();
                        var createForm = document.getElementById("edit-form");
                        if (createForm) {
                            var nodes = createForm.getElementsByTagName('*');
                            for (var i = 0; i < nodes.length; i++) {
                                nodes[i].disabled = true;
                            }
                        }
                    }
                },
                cancel: {
                    text: "Cancel",
                    btnClass: 'btn-default'
                }
            }
        });
    }
});

$(document).ready(function () {

    const currentpassword = $('#NewPassword');
    if (currentpassword) {
        currentpassword.focus()
    }

    $('#editButton').on('click', function (e) {
        $("#edit-form").addClass("submit-progress-bg");

        // Update UI with a short delay to show spinner
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#editButton').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> User Profile");
    });
    $("#edit-form").validate();

});
document.addEventListener('DOMContentLoaded', function () {
    // Add event listeners to all elements with class `toggle-password-visibility`
    const toggleButtons = document.querySelectorAll('.toggle-password-visibility');

    toggleButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            // Get the target input field ID from the `data-target` attribute
            const passwordFieldId = button.getAttribute('data-target');
            const passwordField = document.getElementById(passwordFieldId);
            const eyeIcon = button.querySelector('i');

            // Toggle password visibility
            if (passwordField.type === "password") {
                passwordField.type = "text";
                eyeIcon.classList.remove("fa-eye-slash");
                eyeIcon.classList.add("fa-eye");
            } else {
                passwordField.type = "password";
                eyeIcon.classList.remove("fa-eye");
                eyeIcon.classList.add("fa-eye-slash");
            }
        });
    });



    var pwdModal = document.getElementById('passwordModal');
    var closeTermsButton = document.getElementById('closeterms');
    var pwdLinks = document.querySelectorAll('#update-password-description');


    if (pwdLinks) {
        pwdLinks.forEach(function (termsLink) {
            termsLink.addEventListener('click', function (e) {
                e.preventDefault(); // Prevent default link behavior (i.e., not navigating anywhere)

                // Show the terms modal
                var termsModal = document.querySelector('#passwordModal');
                termsModal.classList.remove('hidden-section');
                termsModal.classList.add('show');
            });
        });
    }

    if (closeTermsButton) {
        closeTermsButton.addEventListener('click', function () {
            pwdModal.classList.add('hidden-section'); // Remove the 'show' class to hide the modal
            pwdModal.classList.remove('show'); // Close the modal if clicked outside
        });
    }


    // Optionally, you can close the modal if clicked outside the modal content
    window.addEventListener('click', function (e) {
        if (e.target === pwdModal) {
            pwdModal.classList.add('hidden-section'); // Remove the 'show' class to hide the modal
            pwdModal.classList.remove('show'); // Close the modal if clicked outside
        }
    });

});
let typingInProgress = false;
let messageQueue = [];
const chatGPTMessage = document.getElementById('password-advise');

if (chatGPTMessage) {
    var userEmail = document.getElementById('Email').value;
    const eventSource = new EventSource(`/Account/StreamTypingUpdates?email=${userEmail}`);

    eventSource.addEventListener('message', (event) => {
        if (event.data === "done") {
            eventSource.close();
        }
        else if (event.data.startsWith("PASSWORD_UPDATE|")) {
            // Extract the JSON data after the separator
            const jsonData = event.data.replace("PASSWORD_UPDATE|", "");
            const passwordModel = JSON.parse(jsonData);

            // Populate UI with user details first
            document.getElementById('displayedEmail').textContent = passwordModel.email;
            document.getElementById('CurrentPassword').value = passwordModel.currentPassword;
            document.getElementById('profilePicture').src = `data:image/png;base64,${passwordModel.profilePicture}`;
        }
        else {
            // Queue messages for typing effect
            messageQueue.push(event.data);
            processNextMessage();
        }
    });
}

// Function to process messages with typing effect
function processNextMessage() {
    if (typingInProgress || messageQueue.length === 0) {
        return;
    }
    typingInProgress = true;
    const message = messageQueue.shift();
    simulateTypingEffect(message, () => {
        typingInProgress = false;
        processNextMessage();
    });
}

// Function to simulate the typing effect
function simulateTypingEffect(message, callback) {
    const messageDiv = document.createElement('div');
    chatGPTMessage.appendChild(messageDiv);
    messageDiv.textContent = '';

    let index = 0;
    const typingInterval = setInterval(() => {
        if (index < message.length) {
            messageDiv.textContent += message[index];
            index++;
        } else {
            clearInterval(typingInterval);
            if (callback) callback();
        }
    }, 100);
}
