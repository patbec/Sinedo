namespace Application.Services {

    export class SystemControl {

        public constructor() {
 
        }

        public updateData(hostname: string, platform: string, arch: string, pid: number, version: string): void {
            document.getElementsByName("info_hostname")
                    .forEach(e => e.innerText = hostname);

            document.getElementsByName("info_platform")
                    .forEach(e => e.innerText = platform.toString());

            document.getElementsByName("info_arch")
                    .forEach(e => e.innerText = arch);

            document.getElementsByName("info_pid")
                    .forEach(e => e.innerText = pid.toString());

            document.getElementsByName("info_version")
                    .forEach(e => e.innerText = version);
        }
    }
}