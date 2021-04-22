

using Signum.Utilities;
using System;
using System.ComponentModel;

namespace Signum.Entities
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class AllowUnathenticatedAttribute : Attribute
    {

    }

    public enum OperationMessage
    {
        [Description("Create...")]
        Create,
        [Description("^Create (.*) from .*$")]
        CreateFromRegex,
        [Description("State should be {0} (instead of {1})")]
        StateShouldBe0InsteadOf1,
        [Description("(in user interface)")]
        InUserInterface,
        [Description("Operation {0} ({1}) is not Authorized")]
        Operation01IsNotAuthorized,
        Confirm,
        [Description("Please confirm you would like to delete {0} from the system")]
        PleaseConfirmYouWouldLikeToDelete0FromTheSystem,
        [Description("{0} didn't return an entity")]
        TheOperation0DidNotReturnAnEntity,
        Logs,
        PreviousOperationLog,
        [Description("{0} & Close")]
        _0AndClose,
        [Description("{0} & New")]
        _0AndNew,

        BulkModifications, 
        [Description("Please confirm that you would like to apply the above changes and execute {0} over {1} {2}")]
        PleaseConfirmThatYouWouldLikeToApplyTheAboveChangesAndExecute0Over12,

        Condition, 
        Setters,
        [Description("Add setter")]
        AddSetter,
        [Description("multi setter")]
        MultiSetter,
    }

    public enum SynchronizerMessage
    {
        [Description("--- END OF SYNC SCRIPT")]
        EndOfSyncScript,
        [Description("--- START OF SYNC SCRIPT GENERATED ON {0}")]
        StartOfSyncScriptGeneratedOn0
    }

    public enum EngineMessage
    {
        [Description("Concurrency error on the database, Table = {0}, Id = {1}")]
        ConcurrencyErrorOnDatabaseTable0Id1,
        [Description("Entity with type {0} and Id {1} not found")]
        EntityWithType0AndId1NotFound,
        [Description("No way of mapping type {0} found")]
        NoWayOfMappingType0Found,
        [Description("The entity {0} is new")]
        TheEntity0IsNew,
        [Description("There are '{0}' that refer to this entity")]
        ThereAre0ThatReferThisEntity,
        [Description("There are records in '{0}' referring to this table by column '{1}'")]
        ThereAreRecordsIn0PointingToThisTableByColumn1,
        [Description("Unauthorized access to {0} because {1}")]
        UnauthorizedAccessTo0Because1,
        [Description("There is already a {0} with {1} equals to {2}")]
        TheresAlreadyA0With1EqualsTo2_G
    }

    public enum NormalWindowMessage
    {
        [Description("{0} Errors: {1}")]
        _0Errors1,
        [Description("1 Error: {0}")]
        _1Error,
        Cancel,
        [Description("Continue anyway?")]
        ContinueAnyway,
        [Description("Continue with errors?")]
        ContinueWithErrors,
        [Description("Fix Errors")]
        FixErrors,
        [Description(@"Impossible to Save, integrity check failed:

")]
        ImpossibleToSaveIntegrityCheckFailed,
        [Description("Loading {0}...")]
        Loading0,
        NoDirectErrors,
        Ok,
        Reload,
        [Description(@"The {0} has errors:
{1}")]
        The0HasErrors1,
        ThereAreErrors,
        Message,
        [Description("New {0}")]
        New0_G,
        [Description("{0} {1}")]
        Type0Id1,
        Main

    }

    public enum EntityControlMessage
    {
        Create,
        Find,
        Detail,
        MoveDown,
        MoveUp,
        Move,
        Navigate,
        Remove,
        View,
        [Description("Add…")]
        Add,

    }

    [DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
    public enum BooleanEnum
    {
        [Description("No")]
        False = 0,
        [Description("Yes")]
        True = 1,
    }

    public enum SearchMessage
    {
        ChooseTheDisplayNameOfTheNewColumn,
        Field,
        [Description("Add column")]
        AddColumn,
        CollectionsCanNotBeAddedAsColumns,
        [Description("Add filter")]
        AddFilter,
        [Description("Add group")]
        AddGroup,
        [Description("Add value")]
        AddValue,
        [Description("Delete filter")]
        DeleteFilter,
        [Description("Delete all filter")]
        DeleteAllFilter,
        Filters,
        Find,
        [Description("Finder of {0}")]
        FinderOf0,
        Name,
        [Description("New column's Name")]
        NewColumnSName,
        NoActionsFound,
        NoColumnSelected,
        NoFiltersSpecified,
        [Description("of")]
        Of,
        Operation,
        [Description("Query {0} is not allowed")]
        Query0IsNotAllowed,
        [Description("Query {0} is not allowed")]
        Query0NotAllowed,
        [Description("Query {0} is not registered in the QueryLogic.Queries")]
        Query0NotRegistered,
        Rename,
        [Description("{0} result[s].")]
        _0Results_N,
        [Description("first {0} result[s].")]
        First0Results_N,
        [Description("{0} - {1} of {2} result[s].")]
        _01of2Results_N,
        Search,
        Refresh,
        Create,
        [Description("Create new {0}")]
        CreateNew0_G,
        [Description("There is no {0}")]
        ThereIsNo0,
        Value,
        View,
        [Description("View Selected")]
        ViewSelected,
        Operations,
        NoResultsFound,
        Explore,
        PinnedFilter,
        Label,
        Column,
        Row,
        SplitText,
        [Description("When pressed, the filter wil take no effect if the value is null")]
        WhenPressedTheFilterWillTakeNoEffectIfTheValueIsNull,
        [Description("When pressed, the filter value will be splited and all the words have to be found")]
        WhenPressedTheFilterValueWillBeSplittedAndAllTheWordsHaveToBeFound,
        ParentValue,
        [Description("Please select a {0}")]
        PleaseSelectA0_G,
        [Description("Please select one or several {0}")]
        PleaseSelectOneOrMore0_G,
        [Description("Please select an Entity")]
        PleaseSelectAnEntity,
        [Description("Please select one or several Entities")]
        PleaseSelectOneOrSeveralEntities,
        [Description("{0} filters collapsed")]
        _0FiltersCollapsed,
        DisplayName,
        [Description("To prevent performance issues automatic search is disabled, check your filters first and then click [Search] button.")]
        ToPreventPerformanceIssuesAutomaticSearchIsDisabledCheckYourFiltersAndThenClickSearchButton,
        [Description("{0} elements")]
        PaginationAll_0Elements,
        [Description("{0} of {1} elements")]
        PaginationPages_0Of01lements,
        [Description("{0} {1} elements")]
        PaginationFirst_01Elements,
        [Description("Return new entity?")]
        ReturnNewEntity,
        [Description("Do you want to return the new {0} ({1})?")]
        DoYouWantToSelectTheNew01_G,
        [Description("Show pinned filter options")]
        ShowPinnedFiltersOptions,
        [Description("Hide pinned filter options")]
        HidePinnedFiltersOptions,
        [Description("Summary header")]
        SummaryHeader,
        [Description("Summary header must be an aggregate (like Sum, Count, etc..)")]
        SummaryHeaderMustBeAnAggregate,

        HiddenColumn,
        ShowHiddenColumns,
        HideHiddenColumns, 

        GroupKey,
        DerivedGroupKey
    }

    public enum SelectorMessage
    {
        [Description("Constructor Selector")]
        ConstructorSelector,
        [Description("Please choose a value to continue:")]
        PleaseChooseAValueToContinue,
        [Description("Please select a constructor")]
        PleaseSelectAConstructor,
        [Description("Please select one of the following types: ")]
        PleaseSelectAType,
        [Description("Type Selector")]
        TypeSelector,
        [Description("A value must be specified for {0}")]
        ValueMustBeSpecifiedFor0,
        ChooseAValue,
        SelectAnElement,
        PleaseSelectAnElement,
        [Description("{0} selector")]
        _0Selector,
        [Description("Please choose a {0} to continue:")]
        PleaseChooseA0ToContinue,
    }

    [AllowUnathenticated]
    public enum ConnectionMessage
    {
        AConnectionWithTheServerIsNecessaryToContinue,
        [Description("Sesion Expired")]
        SessionExpired,
        [Description("A new version has just been deployed! Save changes and {0}")]
        ANewVersionHasJustBeenDeployedSaveChangesAnd0,
        OutdatedClientApplication,
        [Description("Looks like a new version has just been deployed! If you don't have changes that need to be saved, consider reloading")]
        ANewVersionHasJustBeenDeployedConsiderReload,
        Refresh,
    }


    public enum PaginationMessage
    {
        All
    }

    public enum NormalControlMessage
    {
        [Description("View for type {0} is not allowed")]
        ViewForType0IsNotAllowed,
        SaveChangesFirst,
    }

    public enum SaveChangesMessage
    {
        ThereAreChanges,
        YoureTryingToCloseAnEntityWithChanges,
        LoseChanges,
    }

    public enum CalendarMessage
    {
        [Description("Today")]
        Today,
    }

    [AllowUnathenticated]
    public enum JavascriptMessage
    {
        [Description("Choose a type")]
        chooseAType,
        [Description("Choose a value")]
        chooseAValue,
        [Description("Add filter")]
        addFilter,
        [Description("Open tab")]
        openTab,

        [Description("Error")]
        error,
        [Description("Executed")]
        executed,
        [Description("Hide filters")]
        hideFilters,
        [Description("Show filters")]
        showFilters,
        [Description("Group results")]
        groupResults,
        [Description("Ungroup results")]
        ungroupResults,
        [Description("Show group")]
        ShowGroup,
        [Description("Acivate Time Machine")]
        activateTimeMachine,
        [Description("Deactivate Time Machine")]
        deactivateTimeMachine,
        [Description("Show Records")]
        showRecords,
        [Description("Loading...")]
        loading,
        [Description("No actions found")]
        noActionsFound,
        [Description("Save changes before or press cancel")]
        saveChangesBeforeOrPressCancel,
        [Description("Lose current changes?")]
        loseCurrentChanges,
        [Description("No elements selected")]
        noElementsSelected,
        [Description("Search for results")]
        searchForResults,
        [Description("Select only one element")]
        selectOnlyOneElement,
        [Description("There are errors in the entity, do you want to continue?")]
        popupErrors,
        [Description("There are errors in the entity")]
        popupErrorsStop,
        [Description("Insert column")]
        insertColumn,
        [Description("Edit column")]
        editColumn,
        [Description("Remove column")]
        removeColumn,
        [Description("Remove other columns")]
        removeOtherColumns,
        [Description("Restore default columns")]
        restoreDefaultColumns,
        [Description("Saved")]
        saved,
        [Description("Search")]
        search,
        [Description("Selected")]
        Selected,
        [Description("Select a token")]
        selectToken,

        [Description("Find")]
        find,
        [Description("Remove")]
        remove,
        [Description("View")]
        view,
        [Description("Create")]
        create,
        [Description("Move down")]
        moveDown,
        [Description("Move up")]
        moveUp,
        [Description("Navigate")]
        navigate,
        [Description("New entity")]
        newEntity,
        [Description("Ok")]
        ok,
        [Description("Cancel")]
        cancel,
        [Description("Show Period")]
        showPeriod,
        [Description("Show Previous Operation")]
        showPreviousOperation,

        [Description("Date")]
        Date,
    }

    //https://github.com/jquense/react-widgets/blob/5d4985c6dac0df34b86c7d8ad311ff97066977ab/packages/react-widgets/src/messages.tsx#L35
    [AllowUnathenticated]
    public enum ReactWidgetsMessage
    {
        [Description("Today")]
        MoveToday,

        [Description("Navigate back")]
        MoveBack,
        [Description("Navigate forward")]
        MoveForward,
        [Description("Select date")]
        DateButton,
        [Description("open combobox")]
        OpenCombobox,
        [Description("")]
        FilterPlaceholder,
        [Description("There are no items in this list")]
        EmptyList,
        [Description("The filter returned no results")]
        EmptyFilter,
        [Description("Create option")]
        CreateOption,
        [Description("Create option {0}")]
        CreateOption0,
        [Description("Selected items")]
        TagsLabel,
        [Description("Remove selected item")]
        RemoveLabel,
        [Description("no selected items")]
        NoneSelected,
        [Description("Selected items: {0}")]
        SelectedItems0,
        [Description("Increment value")]
        IncrementValue,
        [Description("Decrement value")]
        DecrementValue,
    }

    public enum QuickLinkMessage
    {
        [Description("Quick links")]
        Quicklinks,
        [Description("No {0} found")]
        No0Found
    }

    public enum VoidEnumMessage
    {
        [Description("-")]
        Instance
    }
}
