import { observable } from 'mobx';

export class ColumnModel {

    @observable
    public left: number;

    @observable
    public top = 0;

    constructor(public readonly label: string, left: number) {
        this.left = left;
    }
}
