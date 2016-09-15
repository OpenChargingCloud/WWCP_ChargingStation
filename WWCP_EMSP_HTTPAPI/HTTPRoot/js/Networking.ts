function Download(URI: string, OnSuccess, OnError) {

    var ajax = new XMLHttpRequest();
    ajax.open("GET", URI, true);
    ajax.setRequestHeader("Accept", "application/json; charset=UTF-8");
    //   ajax.setRequestHeader("Content-Type", "application/json; charset=UTF-8");

    ajax.onreadystatechange = function () {

        // 0 UNSENT | 1 OPENED | 2 HEADERS_RECEIVED | 3 LOADING | 4 DONE
        if (this.readyState === 4) {

            // Ok
            if (this.status === 200) {

                //alert(ajax.getAllResponseHeaders());
                //alert(ajax.getResponseHeader("Date"));
                //alert(ajax.getResponseHeader("Cache-control"));
                //alert(ajax.getResponseHeader("ETag"));

                if (OnSuccess && typeof OnSuccess === 'function')
                    OnSuccess(ajax.responseText);

            }

            else if (this.status === 3001)
            { }

            else
                if (OnError && typeof OnError === 'function')
                    OnError(this.status, this.statusText);

        }

    }

    ajax.send();
    //ajax.send("{ \"username\": \"ahzf\" }");

}

function DownloadBlob(URI: string, OnSuccess, OnError) {

    var ajax = new XMLHttpRequest();
    ajax.open("GET", URI, true);
    ajax.setRequestHeader("Accept", "application/json; charset=UTF-8");
    //   ajax.setRequestHeader("Content-Type", "application/json; charset=UTF-8");
    ajax.responseType = 'blob';

    // http://www.html5rocks.com/en/tutorials/file/xhr2/
    ajax.onload = (e) => {

        if (this.status == 200) {

            var blob = new Blob([this.response], { type: 'image/png' });

        }

    };

    ajax.send();
    //ajax.send("{ \"username\": \"ahzf\" }");

}

function sendForm(form) {

    var formData = new FormData(form);

    formData.append('secret_token', '1234567890'); // Append extra data before send.

    var xhr = new XMLHttpRequest();
    xhr.open('POST', form.action, true);
    xhr.onload = (e) => {


    };

    xhr.send(formData);

    return false; // Prevent page from submitting.

}

function uploadFiles(url, files) {

    var formData = new FormData();

    for (var i = 0, file; file = files[i]; ++i) {
        formData.append(file.name, file);
    }

    var xhr = new XMLHttpRequest();
    xhr.open('POST', url, true);
    xhr.onload = function (e) {  };

    xhr.send(formData);  // multipart/form-data
}

//document.querySelector('input[type="file"]').addEventListener('change', function (e) {
//    uploadFiles('/server', this.files);
//}, false);


// <progress min="0" max="100" value="0">0% complete</progress>

// onwaiting: (ev: Event) => any;

function upload(blobOrFile) {

    var xhr = new XMLHttpRequest();
    xhr.open('POST', '/server', true);
    xhr.onload = function (e) { };

    // Listen to the upload progress.
    var progressBar = <HTMLProgressElement> document.querySelector('progress');
    xhr.upload.onprogress = function (e) {
        if (e.lengthComputable) {
            progressBar.value = (e.loaded / e.total) * 100;
            //progressBar.textContent = progressBar.value; // Fallback for unsupported browsers.
        }
    };

    xhr.send(blobOrFile);
}

//upload(new Blob(['hello world'], { type: 'text/plain' }));
