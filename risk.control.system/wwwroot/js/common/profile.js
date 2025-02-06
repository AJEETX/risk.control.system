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
                        $("body").addClass("submit-progress-bg");

                        // Update UI with a short delay to show spinner
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
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
// Flag to keep track of message typing
let typingInProgress = false;
let messageQueue = []; // Queue to store messages and process them in order
const chatGPTMessage = document.getElementById('password-advise');
if (chatGPTMessage) {
    const eventSource = new EventSource('/Account/StreamTypingUpdates');

    

    eventSource.addEventListener('message', (event) => {
        // If the message is "done", we stop the EventSource
        if (event.data === "done") {
            eventSource.close();  // Close the connection once all messages are received
        } else {
            // Add the message to the queue and start processing if not already typing
            messageQueue.push(event.data);
            processNextMessage();  // Try to process the next message
        }
    });
}


// Function to process messages in the queue
function processNextMessage() {
    if (typingInProgress || messageQueue.length === 0) {
        return; // If typing is still in progress or there are no more messages, do nothing
    }

    typingInProgress = true; // Mark typing as in progress
    const message = messageQueue.shift(); // Get the next message from the queue
    simulateTypingEffect(message, () => {
        typingInProgress = false; // Mark typing as finished
        processNextMessage(); // Process the next message in the queue
    });
}

// Function to simulate the typing effect
function simulateTypingEffect(message, callback) {
    // Create a new div to hold each message
    const messageDiv = document.createElement('div');
    chatGPTMessage.appendChild(messageDiv);  // Add the new message to the container
    messageDiv.textContent = '';  // Start with an empty message

    let index = 0;
    const typingInterval = setInterval(() => {
        if (index < message.length) {
            messageDiv.textContent += message[index];
            index++;
        } else {
            clearInterval(typingInterval);  // Stop typing simulation for this message
            if (callback) callback(); // Call the callback once typing is finished
        }
    }, 100); // Adjust typing speed here (100ms per character)
}
