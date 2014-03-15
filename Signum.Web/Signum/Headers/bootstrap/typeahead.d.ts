interface JQuery {

    typeahead(options?: Typeahead.Options, ...dataset: Typeahead.Dataset[]): JQuery;

    typeahead(methodName: 'destroy'): JQuery;
    typeahead(methodName: 'close'): JQuery;
    typeahead(methodName: 'val'): string;
    typeahead(methodName: string): any;

    typeahead(methodName: 'val', val: string): JQuery;
    typeahead(methodName: string, val: string): any;
}

declare module Typeahead {
    interface Dataset {
        source: (query: string, cb: (result: any[]) => void) => void;
        name?: string;
        displayKey?: string;
        templates?: Templates
    }

    interface Templates {
        empty?: (query: string) => string; //or string
        footer?: (query: string) => string; //or string
        header?: (query: string) => string; //or string
        suggestion?: (data: any) => string;
    }

    interface Options {
        highlight?: boolean;
        hint?: boolean;
        minLength?: number;
    }
}
