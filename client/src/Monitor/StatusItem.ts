import { computed, observable } from 'mobx';
import { now } from 'mobx-utils';

export class StatusItem {

    @observable
    private textValue = "";

    @observable
    private changed = new Date().getTime();

    @computed
    public get age() {
        return now(500) - this.changed;
    }

    @computed
    public get text() {
        return this.textValue;
    }
    public set text(v: string) {
        this.textValue = v;
        this.changed = new Date().getTime();
    } 

    constructor(public readonly key: string) { }    
}
