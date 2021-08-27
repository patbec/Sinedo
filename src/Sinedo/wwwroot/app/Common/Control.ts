namespace Application.Common {

    export class Control {

        public static get<T>(name: string): T {
            if (name == null || name == "")
                throw Error(`The ''${name}'' parameter must not be empty.`);

            let control: HTMLElement = document.getElementById(name);

            if (control == null)
                throw Error(`The control with the name '${name}' was not found`);

            let result: T = control as any;

            if (result == null)
                throw Error(`The control element '${name}' has an incorrect type.`);

            return result;
        }
    }
}