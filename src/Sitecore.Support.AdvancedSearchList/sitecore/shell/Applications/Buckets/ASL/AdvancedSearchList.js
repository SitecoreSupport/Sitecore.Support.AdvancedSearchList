
window.ASL = window.ASL || {};

ASL.validState = function() {
    if (window.dataSourceViewType === undefined || window.currentBucketsViewType != window.dataSourceViewType) {
        console.warn("ASL tries to use a wrong view type. ");
        return false;
    }

    if (window.selectedIds === undefined) {
        console.warn("ASL can't find selected ids. ");
        return false;
    }

    return true;
}

ASL.initSelectedIds = function (selectedIds) {
    var self = {};
    self.sIds = selectedIds;

    self.add = function (id) {
        if (this.sIds.indexOf(id) == -1) {
            this.sIds.push(id);
            return true;
        }

        return false;
    };

    self.remove = function (id) {
        var index = this.sIds.indexOf(id);
        if (index >= 0) {
            this.sIds.splice(index, 1);
        }
    };

    return self;
}

ASL.initSearchResult = function (selectedIds) {
    var self = {};
    self.selectedIds = selectedIds;

    self.resultItems = function () {
        return $j("#results").children(".BlogPostArea");
    };

    self.isSelected = function (element) {
        return $j(element).hasClass("highlight");
    }

    self.isElementOfId = function (element, id) {
        return element.id == id;
    }

    self.selectVersion = function (element) {
        $j(element).addClass("highlight");
    }

    self.deselectVersion = function (element) {
        $j(element).removeClass("highlight");
    }

    self.getItemElement = function (element) {
        var el = $j(element);
        if (el.is(".ceebox,.imgcontainer")) {
            return null;
        }

        return el.closest(".BlogPostArea")[0];
    }

    self.select = function (itemId, suppress) {
        var _this = this;
        this.resultItems().each(function (index, element) {
            if (!_this.isElementOfId(element, itemId)) {
                return;
            }
            _this.selectVersion(element);
        });

        if (!suppress) {
            this.selectedIds.add(itemId);
        }
        
    }

    self.deselect = function (itemId) {
        var _this = this;
        this.resultItems().each(function (index, element) {
            if (!_this.isElementOfId(element, itemId)) {
                return;
            }
            _this.deselectVersion(element);
        });

        this.selectedIds.remove(itemId);
    }

    self.highlightAll = function () {
        var _this = this;
        $j(this.selectedIds.sIds).each(
            function (index, id) {
                _this.select(id, true);
            }
        );
    }

    self.bindResult = function () {
        var res = this.selectedIds.sIds.join("|");
        $j('#selectedItems', parent.document.body).val(res);
    }

    self.switch = function (element) {
        var switchOn = !this.isSelected(element);
        if (switchOn) {
            this.select(element.id);
        } else {
            this.deselect(element.id);
        }
    }

    return self;
}

ASL.initialize = function (selectedIds, indexName) {
    if (indexName !== undefined && indexName != null && indexName.length !== 0) {
        window.indexName = indexName;
    }

    var selectedIdsInst = ASL.initSelectedIds(selectedIds);
    
    var searchResult = ASL.initSearchResult(selectedIdsInst);

    window.toggleSelected = function (clickableElement) {
        var element = searchResult.getItemElement(clickableElement);
        if (element == null) {
            return;
        }

        searchResult.switch(element);
        searchResult.bindResult();
    }

    var originalParseResults = window.parseResults;
    window.parseResults = function(resultCallBack) {
        originalParseResults(resultCallBack);
        searchResult.highlightAll();
    }

    // Removes their default implementation
    window.BindItemResult = function (b) {}
    window.BindItemResultDatasource = function () { }

    window.establishViews = function() {
        $j("#views").children().each(function (index, view) {
            if (view.id != "list") {
                view.hide();
            }
        });
    }

    SC.waitFor('SC.libsAreLoaded', function () { ASL.OnLoad(searchResult); });
}

ASL.OnLoad = function (searchResult) {
    searchResult.bindResult();
}

if (ASL.validState()) {
    ASL.initialize(window.selectedIds, window.aslIndex);
}

