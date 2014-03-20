﻿/// <reference path="globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")

export interface EntityBaseOptions {
    prefix: string;
    partialViewName: string;
    template?: string;
    templateToString?: string;

    autoCompleteUrl?: string;

    types: string[];
    typeNiceNames: string[];
    isEmbedded: boolean;
    isReadonly: boolean;
    rootType?: string;
    propertyRoute?: string;
}

export class EntityBase {
    options: EntityBaseOptions;
    element: JQuery;
    hidden: JQuery;
    inputGroup: JQuery;
    shownButton: JQuery;
    autoCompleter: EntityAutocompleter;

    entityChanged: () => void;
    removing: (prefix: string) => Promise<boolean>;
    creating: (prefix: string) => Promise<Entities.EntityValue>;
    finding: (prefix: string) => Promise<Entities.EntityValue>;
    viewing: (entityHtml: Entities.EntityHtml) => Promise<Entities.EntityValue>;

    constructor(element: JQuery, options: EntityBaseOptions) {
        this.element = element;
        this.element.data("SF-control", this);
        this.options = options;
        this.hidden = $(this.pf("hidden"));
        this.inputGroup = $(this.pf("inputGroup"));
        this.shownButton = $(this.pf("shownButton"));
      
        var temp = $(this.pf(Entities.Keys.template));

        if (temp.length > 0) {
            this.options.template = temp.html().replaceAll("<scriptX", "<script").replaceAll("</scriptX", "</script");
            this.options.templateToString = temp.attr("data-toString");
        }

        this.fixInputGroup();

        this._create();
    }

    public ready() {

        this.element.SFControlFullfill(this);
    }

    static key_entity = "sfEntity";

    _create() { //abstract

    }

    runtimeInfoHiddenElement(itemPrefix?: string): JQuery {
        return $(this.pf(Entities.Keys.runtimeInfo));
    }

    pf(sufix : string) {
        return "#" + SF.compose(this.options.prefix, sufix);
    }

    containerDiv(itemPrefix?: string) {
        var containerDivId = this.pf(EntityBase.key_entity);
        if ($(containerDivId).length == 0)
            this.runtimeInfoHiddenElement().after(SF.hiddenDiv(containerDivId.after('#'), ""));

        return $(containerDivId);
    }

    getRuntimeInfo(): Entities.RuntimeInfo
    {
        return Entities.RuntimeInfo.getFromPrefix(this.options.prefix);
    }

    extractEntityHtml(itemPrefix?: string): Entities.EntityHtml {

        var runtimeInfo = Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

        if (runtimeInfo == null)
            return null;

        var div = this.containerDiv();

        var result = new Entities.EntityHtml(this.options.prefix, runtimeInfo,
            this.getToString(),
            this.getLink());

        result.html = div.children();

        div.html(null);

        return result;
    }

    getLink(itemPrefix?: string): string {
        return null;
    }

    getToString(itemPrefix?: string): string {
        return null;
    }


    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        //virtual function
    }

    setEntity(entityValue: Entities.EntityValue, itemPrefix?: string) {

        this.setEntitySpecific(entityValue)

        if (entityValue)
            entityValue.assertPrefixAndType(this.options.prefix, this.options.types);


        this.containerDiv().html(entityValue == null ? null : (<Entities.EntityHtml>entityValue).html);
        Entities.RuntimeInfo.setFromPrefix(this.options.prefix, entityValue == null ? null : entityValue.runtimeInfo);
        if (entityValue == null) {
            Validator.cleanHasError(this.element);
        }

        this.updateButtonsDisplay();
        this.notifyChanges();
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged();
        }
    }

    notifyChanges() {
        SF.setHasChanges(this.element);
    }

    remove_click(): Promise<void> {
        return this.onRemove(this.options.prefix).then(result=> {
            if (result)
                this.setEntity(null);
        });
    }

    onRemove(prefix: string): Promise<boolean> {
        if (this.removing != null)
            return this.removing(prefix);

        return Promise.resolve(true);
    }

    create_click(): Promise<void> {
        return this.onCreating(this.options.prefix).then(result => {
            if (result)
                this.setEntity(result);
        });
    }

    typeChooser(): Promise<string> {
        return Navigator.typeChooser(this.options.prefix,
            this.options.types.map((t, i) => ({ value: t, toStr: this.options.typeNiceNames[i] })));
    }

    singleType(): string {
        if (this.options.types.length != 1)
            throw new Error("There are {0} types in {1}".format(this.options.types.length, this.options.prefix));

        return this.options.types[0];
    }

    onCreating(prefix: string): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix);

        return this.typeChooser().then(type=> {
            if (type == null)
                return null;

            var newEntity = this.options.template ? this.getEmbeddedTemplate(prefix) :
                new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type, null, true), lang.signum.newEntity);

            return Navigator.viewPopup(newEntity, this.defaultViewOptions());
        });
    }

    getEmbeddedTemplate(itemPrefix?: string): Entities.EntityHtml {
        if (!this.options.template)
            throw new Error("no template in " + this.options.prefix);

        var result = new Entities.EntityHtml(this.options.prefix,
            new Entities.RuntimeInfo(this.singleType(), null, true), this.options.templateToString);

        result.loadHtml(this.options.template);

        return result;
    }

    view_click(): Promise<void> {
        var entityHtml = this.extractEntityHtml();

        return this.onViewing(entityHtml).then(result=> {
            if (result)
                this.setEntity(result);
            else
                this.setEntity(entityHtml); //previous entity passed by reference
        });
    }

    onViewing(entityHtml: Entities.EntityHtml): Promise<Entities.EntityValue> {
        if (this.viewing != null)
            return this.viewing(entityHtml);

        return Navigator.viewPopup(entityHtml, this.defaultViewOptions());
    }

    find_click(): Promise<void> {
        return this.onFinding(this.options.prefix).then(result => {
            if (result)
                this.setEntity(result);
        });
    }



    onFinding(prefix: string): Promise<Entities.EntityValue> {
        if (this.finding != null)
            return this.finding(prefix);

        return this.typeChooser().then(type=> {
            if (type == null)
                return null;

            return Finder.find({
                webQueryName: type,
                prefix: prefix,
            });
        });
    }

    defaultViewOptions(): Navigator.ViewPopupOptions {
        return {
            readOnly: this.options.isReadonly,
            partialViewName: this.options.partialViewName,
            validationOptions: {
                rootType: this.options.rootType,
                propertyRoute: this.options.propertyRoute,
            }
        };
    }

    updateButtonsDisplay() {

        var hasEntity = !!Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

        this.visibleButton("btnCreate", !hasEntity);
        this.visibleButton("btnFind", !hasEntity);
        this.visibleButton("btnView", hasEntity);
        this.visibleButton("btnRemove", hasEntity);

        this.fixInputGroup();
    }

    fixInputGroup() {
        this.inputGroup.toggleClass("input-group", !!this.shownButton.children().length);
    }

    visibleButton(sufix: string, visible: boolean) {

        var element = $(this.pf(sufix));

        if (!element.length)
            return;

        (visible ? this.shownButton : this.hidden).append(element.detach());
    }

    setupAutocomplete($txt : JQuery) {

        var handler : number;
        var auto = $txt.typeahead({
            hint: false,
            highlight: true,
        }, {
            name: "autocmplete",
            displayKey: "toStr",
            templates: {
                suggestions: (item: Entities.EntityValue) => $("<div>").append(
                    $("p")
                        .attr("data-type", item.runtimeInfo.type)
                        .attr("data-id", item.runtimeInfo.id)
                        .text(item.toStr)).html()
            },

            source: (query, response) => {
                if (handler)
                    clearTimeout(handler);

                handler = setTimeout(() => {
                    this.autoCompleter.getResults(query)
                        .then(entities=> response(entities));
                }, 300);
            },
        });

        $txt.on("typeahead:selected", (event: JQueryEventObject, val: Entities.EntityValue, name: string) => {
            this.onAutocompleteSelected(val);
        }); 
    }

    onAutocompleteSelected(entityValue: Entities.EntityValue) {
        throw new Error("onAutocompleteSelected is abstract");
    }
}

export interface EntityAutocompleter {
    getResults(term: string): Promise<Entities.EntityValue[]>;
}

export interface AutocompleteResult {
    id: number;
    text: string;
    type: string;
    link: string;
}

export class AjaxEntityAutocompleter implements EntityAutocompleter {

    controllerUrl: string;

    getData: (term: string) => any;

    constructor(controllerUrl: string, getData: (term: string) => any) {
        this.controllerUrl = controllerUrl;
        this.getData = getData;
    }

    lastXhr: JQueryXHR; //To avoid previous requests results to be shown

    getResults(term: string): Promise<Entities.EntityValue[]> {
        if (this.lastXhr)
            this.lastXhr.abort();

        return new Promise<Entities.EntityValue[]>((resolve, failure) => {
            this.lastXhr = $.ajax({
                url: this.controllerUrl,
                data: this.getData(term),
                success: function (data: AutocompleteResult[]) {
                    this.lastXhr = null;
                    var entities = data.map(item=> new Entities.EntityValue(new Entities.RuntimeInfo(item.type, item.id, false), item.text, item.link));
                    resolve(entities);
                }
            });
        });
    }

}

export class EntityLine extends EntityBase {

    _create() {
        var $txt = $(this.pf(Entities.Keys.toStr) + ".sf-entity-autocomplete");
        if ($txt.length > 0) {
            this.autoCompleter = new AjaxEntityAutocompleter(this.options.autoCompleteUrl || SF.Urls.autocomplete,
                term => ({ types: this.options.types.join(","), l: 5, q: term }));

            this.setupAutocomplete($txt);

            var inputGroup = this.shownButton.parent();

            var typeahead = $txt.parent();

            var parts = typeahead.children().addClass("typeahead-parts").detach();

            if (typeahead.parent().hasClass("hide"))
                parts.appendTo(typeahead.parent());
            else
                parts.insertBefore(this.shownButton);

            typeahead.remove();
        }
    }

    getLink(itemPrefix?: string): string {
        return $(this.pf(Entities.Keys.link)).attr("href");
    }

    getToString(itemPrefix?: string): string {
        return $(this.pf(Entities.Keys.link)).text();
    }

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        var link = $(this.pf(Entities.Keys.link));
        link.text(entityValue == null ? null : entityValue.toStr);
        if (link.filter('a').length !== 0)
            link.attr('href', entityValue == null ? null : entityValue.link);
        $(this.pf(Entities.Keys.toStr)).val('');

        this.visible($(this.pf(Entities.Keys.link)), entityValue != null);
        this.visible($(this.pf(Entities.Keys.toStr)), entityValue == null); //embedded entities is alone
        this.visible($(this.pf(Entities.Keys.toStr)).siblings(".typeahead-parts"), entityValue == null);
    }

    visible(element : JQuery, visible: boolean) {
        if (!element.length)
            return;

        if (visible)
            this.shownButton.before(element.detach());
        else
            this.hidden.append(element.detach());
    }

    onAutocompleteSelected(entityValue: Entities.EntityValue) {
        this.setEntity(entityValue);
    }
}

export class EntityCombo extends EntityBase {

    static key_combo = "sfCombo";

    combo() {
        return $(this.pf(EntityCombo.key_combo));
    }

    setEntitySpecific(entityValue: Entities.EntityValue) {
        var c = this.combo();

        if (entityValue == null)
            c.val(null);
        else {
            var o = c.children("option[value='" + entityValue.runtimeInfo.key() + "']");
            if (o.length == 1)
                o.html(entityValue.toStr);
            else
                c.add($("<option value='{0}'/>".format(entityValue.runtimeInfo.key())).text(entityValue.toStr));

            c.val(entityValue.runtimeInfo.key());
        }
    }

    getToString(itemPrefix?: string): string {
        return this.combo().children("option[value='" + this.combo().val() + "']").text();
    }

    combo_selected() {
        var val = this.combo().val();

        var ri = Entities.RuntimeInfo.fromKey(val);

        this.setEntity(ri == null ? null : new Entities.EntityValue(ri, this.getToString()));
    }
}

export class EntityLineDetail extends EntityBase {

    options: EntityBaseOptions;

    constructor(element: JQuery, options: EntityBaseOptions) {
        super(element, options);
    }

    containerDiv(itemPrefix?: string) {
        return $(this.pf("sfDetail"));
    }

    fixInputGroup() {
    }

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        if (entityValue == null)
            return;

        if (!entityValue.isLoaded())
            throw new Error("EntityLineDetail requires a loaded Entities.EntityHtml, consider calling Navigator.loadPartialView");
    }

    onCreating(prefix: string): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix);

        if (this.options.template)
            return Promise.resolve(this.getEmbeddedTemplate(prefix));

        return this.typeChooser().then(type=> {
            if (type == null)
                return null;

            var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type, null, true), lang.signum.newEntity);

            return Navigator.requestPartialView(newEntity, this.defaultViewOptions());
        });
    }

    find_click(): Promise<void> {
        return this.onFinding(this.options.prefix).then(result => {
            if (result == null)
                return null;

            if (result.isLoaded())
                return Promise.resolve(<Entities.EntityHtml>result);

            return Navigator.requestPartialView(new Entities.EntityHtml(this.options.prefix, result.runtimeInfo), this.defaultViewOptions());
        }).then(result => {
                if (result)
                    this.setEntity(result);
            });
    }
}



export interface EntityListBaseOptions extends EntityBaseOptions {
    maxElements?: number;
    remove?: boolean;
    reorder?: boolean;
}

export class EntityListBase extends EntityBase {
    static key_indexes = "sfIndexes";

    options: EntityListBaseOptions;
    finding: (prefix: string) => Promise<Entities.EntityValue>;  // DEPRECATED!
    findingMany: (prefix: string) => Promise<Entities.EntityValue[]>;

    constructor(element: JQuery, options: EntityListBaseOptions) {
        super(element, options);
    }

    runtimeInfo(itemPrefix?: string): JQuery {
        return $("#" + SF.compose(itemPrefix, Entities.Keys.runtimeInfo));
    }

    containerDiv(itemPrefix?: string): JQuery {
        var containerDivId = "#" + SF.compose(itemPrefix, EntityList.key_entity);
        if ($(containerDivId).length == 0)
            this.runtimeInfo(itemPrefix).after(SF.hiddenDiv(containerDivId.after("#"), ""));

        return $(containerDivId);
    }

    getEmbeddedTemplate(itemPrefix?: string) {
        if (!this.options.template)
            throw new Error("no template in " + this.options.prefix);

        var result = new Entities.EntityHtml(itemPrefix,
            new Entities.RuntimeInfo(this.singleType(), null, true), this.options.templateToString);

        var replaced = this.options.template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), itemPrefix)

        result.loadHtml(replaced);

        return result;
    }

    extractEntityHtml(itemPrefix?: string): Entities.EntityHtml {
        var runtimeInfo = Entities.RuntimeInfo.getFromPrefix(itemPrefix);

        var div = this.containerDiv(itemPrefix);

        var result = new Entities.EntityHtml(itemPrefix, runtimeInfo,
            this.getToString(itemPrefix),
            this.getLink(itemPrefix));

        result.html = div.children();

        div.html(null);

        return result;
    }

    setEntity(entityValue: Entities.EntityValue, itemPrefix?: string) {
        if (entityValue == null)
            throw new Error("entityValue is mandatory on setEntityItem");

        this.setEntitySpecific(entityValue, itemPrefix)

        entityValue.assertPrefixAndType(itemPrefix, this.options.types);

        if (entityValue.isLoaded())
            this.containerDiv(itemPrefix).html((<Entities.EntityHtml>entityValue).html);

        Entities.RuntimeInfo.setFromPrefix(itemPrefix, entityValue.runtimeInfo);

        this.updateButtonsDisplay();
        this.notifyChanges();
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged();
        }
    }

    create_click(): Promise<void> {
        var itemPrefix = this.getNextPrefix();
        return this.onCreating(itemPrefix).then(entity => {
            if (entity)
                this.addEntity(entity, itemPrefix);
        });
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        //virtual
    }

    addEntity(entityValue: Entities.EntityValue, itemPrefix: string) {
        if (entityValue == null)
            throw new Error("entityValue is mandatory on setEntityItem");

        this.addEntitySpecific(entityValue, itemPrefix);

        if (entityValue)
            entityValue.assertPrefixAndType(itemPrefix, this.options.types);

        if (entityValue.isLoaded())
            this.containerDiv(itemPrefix).html((<Entities.EntityHtml>entityValue).html);
        Entities.RuntimeInfo.setFromPrefix(itemPrefix, entityValue.runtimeInfo);

        this.updateButtonsDisplay();
        this.notifyChanges();
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged();
        }
    }

    removeEntitySpecific(itemPrefix: string) {
        //virtual
    }

    removeEntity(itemPrefix: string) {
        this.removeEntitySpecific(itemPrefix);

        this.updateButtonsDisplay();
        this.notifyChanges();
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged();
        }
    }

    itemSuffix(): string {
        throw new Error("itemSuffix is abstract");
    }

    getItems(): JQuery {
        throw new Error("getItems is abstract");
    }

    getPrefixes(): string[] {
        return this.getItems().toArray()
            .map((e: HTMLElement) => e.id.before("_" + this.itemSuffix()));
    }

    getRuntimeInfos(): Entities.RuntimeInfo[] {
        return this.getPrefixes().map(p=> Entities.RuntimeInfo.getFromPrefix(p));
    }

    getNextPrefix(inc: number = 0): string {

        var indices = this.getItems().toArray()
            .map((e: HTMLElement) => parseInt(e.id.after(this.options.prefix + "_").before("_" + this.itemSuffix())));

        var next: number = indices.length == 0 ? inc :
            (Math.max.apply(null, indices) + 1 + inc);

        return SF.compose(this.options.prefix, next.toString());
    }

    getLastPosIndex(): number {
        var $last = this.getItems().filter(":last");
        if ($last.length == 0) {
            return -1;
        }

        var lastId = $last[0].id;
        var lastPrefix = lastId.substring(0, lastId.indexOf(this.itemSuffix()) - 1);

        return this.getPosIndex(lastPrefix);
    }

    getNextPosIndex(): string {
        return ";" + (this.getLastPosIndex() + 1).toString();
    }

    canAddItems() {
        return SF.isEmpty(this.options.maxElements) || this.getItems().length < this.options.maxElements;
    }

    find_click(): Promise<void> {
        return this.onFindingMany(this.options.prefix).then(result => {
            if (result)
                result.forEach(ev=> this.addEntity(ev, this.getNextPrefix()));
        });
    }

    onFinding(prefix: string): Promise<Entities.EntityValue> {
        throw new Error("onFinding is deprecated in EntityListBase");
    }

    onFindingMany(prefix: string): Promise<Entities.EntityValue[]> {
        if (this.findingMany != null)
            return this.findingMany(prefix);

        return this.typeChooser().then(type=> {
            if (type == null)
                return null;

            return Finder.findMany({
                webQueryName: type,
                prefix: prefix,
            });
        });
    }

    moveUp(itemPrefix: string) {

        var suffix = this.itemSuffix();
        var $item = $("#" + SF.compose(itemPrefix, suffix));
        var $itemPrev = $item.prev();

        if ($itemPrev.length == 0) {
            return;
        }

        var itemPrevPrefix = $itemPrev[0].id.before("_" + suffix);

        var prevNewIndex = this.getPosIndex(itemPrevPrefix);
        this.setPosIndex(itemPrefix, prevNewIndex);
        this.setPosIndex(itemPrevPrefix, prevNewIndex + 1);

        $item.insertBefore($itemPrev);
    }

    moveDown(itemPrefix: string) {

        var suffix = this.itemSuffix();
        var $item = $("#" + SF.compose(itemPrefix, suffix));
        var $itemNext = $item.next();

        if ($itemNext.length == 0) {
            return;
        }

        var itemNextPrefix = $itemNext[0].id.before("_" + suffix);

        var nextNewIndex = this.getPosIndex(itemNextPrefix);
        this.setPosIndex(itemPrefix, nextNewIndex);
        this.setPosIndex(itemNextPrefix, nextNewIndex - 1);

        $item.insertAfter($itemNext);
    }

    getPosIndex(itemPrefix: string) {
        return parseInt($("#" + SF.compose(itemPrefix, EntityListBase.key_indexes)).val().after(";"));
    }

    setPosIndex(itemPrefix: string, newIndex: number) {
        var $indexes = $("#" + SF.compose(itemPrefix, EntityListBase.key_indexes));
        $indexes.val($indexes.val().before(";") + ";" + newIndex.toString());
    }
}

export class EntityList extends EntityListBase {

    static key_list = "sfList";

    _create() {
        var list = $(this.pf(EntityList.key_list));

        list.change(() => this.selection_Changed());

        if (list.height() < this.shownButton.height())
            list.css("min-height", this.shownButton.height());

        this.selection_Changed();
    }

    selection_Changed() {
        this.updateButtonsDisplay();
    }

    itemSuffix() {
        return Entities.Keys.toStr;
    }

    updateLinks(newToStr: string, newLink: string, itemPrefix?: string) {
        $('#' + SF.compose(itemPrefix, Entities.Keys.toStr)).html(newToStr);
    }

    selectedItemPrefix(): string {
        var $items = this.getItems().filter(":selected");
        if ($items.length == 0) {
            return null;
        }

        var nameSelected = $items[0].id;
        return nameSelected.before("_" + this.itemSuffix());
    }

    getItems(): JQuery {
        return $(this.pf(EntityList.key_list) + " > option");
    }

    view_click(): Promise<void> {
        var selectedItemPrefix = this.selectedItemPrefix();

        var entityHtml = this.extractEntityHtml(selectedItemPrefix);

        return this.onViewing(entityHtml).then(result=> {
            if (result)
                this.setEntity(result, selectedItemPrefix);
            else
                this.setEntity(entityHtml, selectedItemPrefix); //previous entity passed by reference
        });
    }



    updateButtonsDisplay() {
        var canAdd = this.canAddItems();
        this.visibleButton("btnCreate", canAdd);
        this.visibleButton("btnFind", canAdd);

        var hasSelected = this.selectedItemPrefix() != null;
        this.visibleButton("btnView", hasSelected);
        this.visibleButton("btnRemove", hasSelected);
        this.visibleButton("btnUp", hasSelected);
        this.visibleButton("btnDown", hasSelected);

        this.fixInputGroup();
    }

    getToString(itemPrefix?: string): string {
        return $("#" + SF.compose(itemPrefix, Entities.Keys.toStr)).text();
    }

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        $("#" + SF.compose(itemPrefix, Entities.Keys.toStr)).text(entityValue.toStr);
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {

        this.inputGroup.before(SF.hiddenInput(SF.compose(itemPrefix, EntityList.key_indexes), this.getNextPosIndex()));
        this.inputGroup.before(SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), entityValue.runtimeInfo.toString()));
        this.inputGroup.before(SF.hiddenDiv(SF.compose(itemPrefix, EntityList.key_entity), ""));

        var select = $(this.pf(EntityList.key_list));
        select.children('option').attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select

        $("<option/>")
            .attr("id", SF.compose(itemPrefix, Entities.Keys.toStr))
            .attr("value", "")
            .attr('selected', true)
            .text(entityValue.toStr)
            .appendTo(select);
    }

    remove_click(): Promise<void> {
        var selectedItemPrefix = this.selectedItemPrefix();
        return this.onRemove(selectedItemPrefix).then(result=> {
            if (result) {
                var next = this.getItems().filter(":selected").next();
                if (next.length == 0)
                    next = this.getItems().filter(":selected").prev();

                this.removeEntity(selectedItemPrefix);

                next.attr("selected", "selected");
                this.selection_Changed();
            }
        });
    }

    removeEntitySpecific(itemPrefix: string) {
        $("#" + SF.compose(itemPrefix, Entities.Keys.runtimeInfo)).remove();
        $("#" + SF.compose(itemPrefix, Entities.Keys.toStr)).remove();
        $("#" + SF.compose(itemPrefix, EntityList.key_entity)).remove();
        $("#" + SF.compose(itemPrefix, EntityList.key_indexes)).remove();
    }

    moveUp_click() {
        this.moveUp(this.selectedItemPrefix());
    }

    moveDown_click() {
        this.moveDown(this.selectedItemPrefix());
    }
}

export interface EntityListDetailOptions extends EntityListBaseOptions {
    detailDiv: string;
}

export class EntityListDetail extends EntityList {

    options: EntityListDetailOptions;

    selection_Changed() {
        super.selection_Changed();
        this.stageCurrentSelected();
    }

    remove_click() {
        return super.remove_click().then(() => this.stageCurrentSelected())
    }

    create_click() {
        return super.create_click().then(() => this.stageCurrentSelected())
    }

    find_click() {
        return super.find_click().then(() => this.stageCurrentSelected())
    }

    stageCurrentSelected() {
        var selPrefix = this.selectedItemPrefix();

        var detailDiv = $("#" + this.options.detailDiv)

        var children = detailDiv.children();
        if (children.length) {
            var itemPrefix = children[0].id.before("_" + EntityListDetail.key_entity);
            if (selPrefix == itemPrefix) {
                children.show();
                return;
            }
        }

        if (selPrefix) {
            var selContainer = this.containerDiv(selPrefix);
            if (selContainer.children().length > 0) {
                children.hide();
                this.runtimeInfo(itemPrefix).after(children);
                detailDiv.append(selContainer);
                selContainer.show();
            } else {
                var entity = new Entities.EntityHtml(selPrefix, Entities.RuntimeInfo.getFromPrefix(selPrefix), null, null);

                Navigator.requestPartialView(entity, this.defaultViewOptions()).then(e=> {
                    selContainer.html(e.html);
                    children.hide();
                    this.runtimeInfo(itemPrefix).after(children);
                    detailDiv.append(selContainer);
                    selContainer.show();
                });
            }
        } else {
            children.hide();
            this.runtimeInfo(itemPrefix).after(children);
        }
    }

    onCreating(prefix: string): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix);

        if (this.options.template)
            return Promise.resolve(this.getEmbeddedTemplate(prefix));

        return this.typeChooser().then(type=> {
            if (type == null)
                return null;

            var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type, null, true), lang.signum.newEntity);

            return Navigator.requestPartialView(newEntity, this.defaultViewOptions());
        });
    }
}

export class EntityRepeater extends EntityListBase {
    static key_itemsContainer = "sfItemsContainer";
    static key_repeaterItem = "sfRepeaterItem";
    static key_repeaterItemClass = "sf-repeater-element";

    itemSuffix() {
        return EntityRepeater.key_repeaterItem;
    }

    fixInputGroup() {
    }

    getItems() {
        return $(this.pf(EntityRepeater.key_itemsContainer) + " > ." + EntityRepeater.key_repeaterItemClass);
    }

    removeEntitySpecific(itemPrefix: string) {
        $("#" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem)).remove();
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        var fieldSet = $("<fieldset id='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' class='" + EntityRepeater.key_repeaterItemClass + "'>" +
            "<legend><div class='item-group'>" +
            (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');" + "\" class='sf-line-button sf-remove'><span class='glyphicon glyphicon-remove'></span></a>") : "") +
            (this.options.reorder ? ("<a id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getRepeaterCall() + ".moveUp('" + itemPrefix + "');" + "\" class='sf-line-button move-up'><span class='glyphicon glyphicon-chevron-up'></span></span></a>") : "") +
            (this.options.reorder ? ("<a id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getRepeaterCall() + ".moveDown('" + itemPrefix + "');" + "\" class='sf-line-button move-down'><span class='glyphicon glyphicon-chevron-down'></span></span></a>") : "") +
            "</div></legend>" +
            SF.hiddenInput(SF.compose(itemPrefix, EntityListBase.key_indexes), this.getNextPosIndex()) +
            SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), null) +
            "<div id='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' class='sf-line-entity'>" +
            "</div>" + //sfEntity
            "</fieldset>"
            );

        $(this.pf(EntityRepeater.key_itemsContainer)).append(fieldSet);
    }

    getRepeaterCall() {
        return "$('#" + this.options.prefix + "').data('SF-control')";
    }

    remove_click(): Promise<void> { throw new Error("remove_click is deprecated in EntityRepeater"); }

    removeItem_click(itemPrefix: string): Promise<void> {
        return this.onRemove(itemPrefix).then(result=> {
            if (result)
                this.removeEntity(itemPrefix);
        });
    }

    onCreating(prefix: string): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix);

        if (this.options.template)
            return Promise.resolve(this.getEmbeddedTemplate(prefix));

        return this.typeChooser().then(type=> {
            if (type == null)
                return null;

            var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type, null, true), lang.signum.newEntity);

            return Navigator.requestPartialView(newEntity, this.defaultViewOptions());
        });
    }

    find_click(): Promise<void> {
        return this.onFindingMany(this.options.prefix)
            .then(result => {
                if (!result)
                    return;

                Promise.all(result
                    .map((e, i) => ({ entity: e, prefix: this.getNextPrefix(i) }))
                    .map(t => {
                        var promise = t.entity.isLoaded() ? Promise.resolve(<Entities.EntityHtml>t.entity) :
                            Navigator.requestPartialView(new Entities.EntityHtml(t.prefix, t.entity.runtimeInfo), this.defaultViewOptions())

                        return promise.then(ev=> this.addEntity(ev, t.prefix));
                    }));
            });
    }

    updateButtonsDisplay() {
        var canAdd = this.canAddItems();

        $(this.pf("btnCreate")).toggle(canAdd);
        $(this.pf("btnFind")).toggle(canAdd);
    }
}

export class EntityTabRepeater extends EntityRepeater {
    static key_tabsContainer = "sfTabsContainer";

    _create() {
        super._create();

        $(this.pf(EntityTabRepeater.key_tabsContainer)).tab();
    }

    itemSuffix() {
        return EntityTabRepeater.key_repeaterItem;
    }


    getItems() {
        return $(this.pf(EntityTabRepeater.key_itemsContainer) + " > ." + EntityTabRepeater.key_repeaterItemClass);
    }

    removeEntitySpecific(itemPrefix: string) {
        $("#" + SF.compose(itemPrefix, EntityTabRepeater.key_repeaterItem)).remove();
        $("#" + SF.compose(itemPrefix, EntityBase.key_entity)).remove();
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        var header = $("<li id='" + SF.compose(itemPrefix, EntityTabRepeater.key_repeaterItem) + "' class='" + EntityTabRepeater.key_repeaterItemClass + "'>" +
            "<a data-toggle='tab' href='#" + SF.compose(itemPrefix, EntityBase.key_entity) + "' >" +
            "<span>" + entityValue.toStr + "</span>" +
            SF.hiddenInput(SF.compose(itemPrefix, EntityListBase.key_indexes), this.getNextPosIndex()) +
            SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), null) +
            (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getRepeaterCall() + ".moveUp('" + itemPrefix + "');" + "\" class='sf-line-button move-up'><span class='glyphicon glyphicon-chevron-left'></span></span>") : "") +
            (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getRepeaterCall() + ".moveDown('" + itemPrefix + "');" + "\" class='sf-line-button move-down'><span class='glyphicon glyphicon-chevron-right'></span></span>") : "") +
            (this.options.remove ? ("<span id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');" + "\" class='sf-line-button sf-remove' ><span class='glyphicon glyphicon-remove'></span></span>") : "") +
            "</a>" +
            "</li>"
            );

        $(this.pf(EntityTabRepeater.key_itemsContainer)).append(header);

        var entity = $("<div id='" + SF.compose(itemPrefix, EntityTabRepeater.key_entity) + "' class='tab-pane'>" +
            "</div>");

        $(this.pf(EntityTabRepeater.key_tabsContainer)).append(entity);

        header.tab("show");
    }

    getRepeaterCall() {
        return "$('#" + this.options.prefix + "').data('SF-control')";
    }
}

export interface EntityStripOptions extends EntityBaseOptions {
    maxElements?: number;
    remove?: boolean;
    vertical?: boolean;
    reorder?: boolean;
    view?: boolean;
    navigate?: boolean;
}

export class EntityStrip extends EntityList {
    static key_itemsContainer = "sfItemsContainer";
    static key_stripItem = "sfStripItem";
    static key_stripItemClass = "sf-strip-element";
    static key_input = "sf-strip-input";

    options: EntityStripOptions;

    constructor(element: JQuery, options: EntityStripOptions) {
        super(element, options);
    }

    fixInputGroup() {
    }

    _create() {
        var $txt = $(this.pf(Entities.Keys.toStr) + ".sf-entity-autocomplete");
        if ($txt.length > 0) {
            this.autoCompleter = new AjaxEntityAutocompleter(this.options.autoCompleteUrl || SF.Urls.autocomplete,
                term => ({ types: this.options.types.join(","), l: 5, q: term }));

            this.setupAutocomplete($txt);

            var inputGroup = this.shownButton.parent();

            var typeahead = $txt.parent();

            var parts = typeahead.children().addClass("typeahead-parts").detach();

            parts.insertBefore(this.shownButton);

            typeahead.remove();
        }
    }

    itemSuffix() {
        return EntityStrip.key_stripItem;
    }

    getItems() {
        return $(this.pf(EntityStrip.key_itemsContainer) + " > ." + EntityStrip.key_stripItemClass);
    }

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        var link = $('#' + SF.compose(itemPrefix, Entities.Keys.link));
        link.text(entityValue.toStr);
        if (this.options.navigate)
            link.attr("href", entityValue.link);
    }

    getLink(itemPrefix?: string): string {
        return $('#' + SF.compose(itemPrefix, Entities.Keys.link)).attr("hef");
    }

    getToString(itemPrefix?: string): string {
        return $('#' + SF.compose(itemPrefix, Entities.Keys.link)).text();
    }

    removeEntitySpecific(itemPrefix: string) {
        $("#" + SF.compose(itemPrefix, EntityStrip.key_stripItem)).remove();
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {

        var itemGroup = this.options.vertical ? "input-group" : "";
        var itemGroupBtn = this.options.vertical ? "input-group-btn" : "";
        var formControl = this.options.vertical ? "form-control" : "";
        var btnDefault = this.options.vertical ? "btn btn-default" : "";


        var li = $("<li id='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' class='" + EntityStrip.key_stripItemClass + " " + itemGroup + "'>" +
            (this.options.navigate ?
            ("<a class='sf-entitStrip-link " + formControl + "' id='" + SF.compose(itemPrefix, Entities.Keys.link) + "' href='" + entityValue.link + "' title='" + lang.signum.navigate + "'>" + entityValue.toStr + "</a>") :
            ("<span class='sf-entitStrip-link " + formControl + "' id='" + SF.compose(itemPrefix, Entities.Keys.link) + "'>" + entityValue.toStr + "</span>")) +
            SF.hiddenInput(SF.compose(itemPrefix, EntityStrip.key_indexes), this.getNextPosIndex()) +
            SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), null) +
            "<div id='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' style='display:none'></div>" +
            "<span class='" + itemGroupBtn + "'>" + (
            (this.options.reorder ? ("<a id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getRepeaterCall() + ".moveUp('" + itemPrefix + "');" + "\" class='" + btnDefault + " sf-line-button move-up'><span class='glyphicon glyphicon-chevron-" + (this.options.vertical ? "up" : "left") + "'></span></a>") : "") +
            (this.options.reorder ? ("<a id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getRepeaterCall() + ".moveDown('" + itemPrefix + "');" + "\" class='" + btnDefault + " sf-line-button move-down'><span class='glyphicon glyphicon-chevron-" + (this.options.vertical ? "down" : "right") + "'></span></a>") : "") +
            (this.options.view ? ("<a id='" + SF.compose(itemPrefix, "btnView") + "' title='" + lang.signum.view + "' onclick=\"" + this.getRepeaterCall() + ".view_click('" + itemPrefix + "');" + "\" class='" + btnDefault + " sf-line-button sf-view'><span class='glyphicon glyphicon-arrow-right'></span></a>") : "") +
            (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');" + "\" class='" + btnDefault + " sf-line-button sf-remove'><span class='glyphicon glyphicon-remove'></span></a>") : "")) +
            "</span>" +
            "</li>" 
            );

        $(this.pf(EntityStrip.key_itemsContainer) + " ." + EntityStrip.key_input).before(li);

    }

    private getRepeaterCall() {
        return "$('#" + this.options.prefix + "').data('SF-control')";
    }

    remove_click(): Promise<void> { throw new Error("remove_click is deprecated in EntityRepeater"); }

    removeItem_click(itemPrefix: string): Promise<void> {
        return this.onRemove(itemPrefix).then(result=> {
            if (result)
                this.removeEntity(itemPrefix);
        });
    }

    view_click(): Promise<void> { throw new Error("remove_click is deprecated in EntityRepeater"); }

    viewItem_click(itemPrefix: string): Promise<void> {
        var entityHtml = this.extractEntityHtml(itemPrefix);

        return this.onViewing(entityHtml).then(result=> {
            if (result)
                this.setEntity(result, itemPrefix);
            else
                this.setEntity(entityHtml, itemPrefix); //previous entity passed by reference
        });
    }

    updateButtonsDisplay() {
        var canAdd = this.canAddItems();

        $(this.pf("btnCreate")).toggle(canAdd);
        $(this.pf("btnFind")).toggle(canAdd);
        $(this.pf("sfToStr")).toggle(canAdd);
    }

    onAutocompleteSelected(entityValue: Entities.EntityValue) {
        this.addEntity(entityValue, this.getNextPrefix());
        $(this.pf(Entities.Keys.toStr) + ".sf-entity-autocomplete").typeahead('val', '');
    }
}
