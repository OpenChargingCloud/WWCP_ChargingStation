self.addEventListener('message', function (e) {
    var data = e.data;
    var worker = self;
    switch (data.cmd) {
        case 'start':
            worker.postMessage('WORKER STARTED: ' + data.msg);
            break;
        case 'stop':
            worker.postMessage('WORKER STOPPED: ' + data.msg);
            break;
        case 'RemoteStart':
            worker.postMessage('Remote start of: ' + data.EVSEId);
            break;
        default:
            worker.postMessage('Unkown command: ' + data.msg);
    }
    ;
}, false);
//# sourceMappingURL=HTTPWorker.js.map