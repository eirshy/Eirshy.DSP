using System;
using System.Collections.Generic;
using System.Linq;

using ItemCount = (Eirshy.DSP.Rythmn.Utilities.Item Item, int Count);
using IdCount = (int Id, int Count);

namespace Eirshy.DSP.Rythmn.Utilities {
    public struct Chef {
        const int MAX_INDEX = 5;

        public RecipeProto Recipe { get; private set; }

        public readonly Item Item => Item._TryGetValue(Id);
        public int Id { get; private set; }
        public int Count { get; private set; }
        public int At { get; private set; }
        public bool IsInput { get; private set; }
        public bool IsResult {
            readonly get => !IsInput;
            set => IsInput = !value;
        }

        readonly int[] _ids => IsInput ? Recipe?.Items : Recipe?.Results;
        readonly int[] _counts => IsInput ? Recipe?.ItemCounts : Recipe?.ResultCounts;

        /// <summary>
        /// If True, our subproperties are wrong in some way.
        /// </summary>
        public bool IsFaulted { get; private set; }

        public readonly bool IsBound => At >= 0 && At < _ids.Length;

        private Chef(RecipeProto recipe) {
            Recipe = recipe;
            WashHands();
        }
        public static explicit operator Chef(RecipeProto recipe) => new(recipe);
        public static implicit operator RecipeProto(Chef chef) => chef.Recipe;

        public readonly Chef RawEdit(Action<RecipeProto> edit) {
            if(IsFaulted) return this;
            edit(Recipe);
            return this;
        }

        /// <summary>
        /// Unsets all of our current data values.
        /// </summary>
        public Chef WashHands() {
            At = -1;
            Id = 0;
            Count = 0;
            IsFaulted = Recipe == null;
            return this;
        }
        /// <summary>
        /// Cleans up any 0-id entries in the recipe.
        /// </summary>
        public Chef CleanRecipe() {
            var recipe = Recipe;
            if(recipe is null) return this;

            if(recipe.Items.Contains(0)) {
                List<IdCount> keep = recipe.Items
                    .Select((id, i) => (id, recipe.ItemCounts[i]))
                    .Where(x => x.id != 0)
                    .ToList()
                ;
                recipe.Items = keep.Select(x => x.Id).ToArray();
                recipe.ItemCounts = keep.Select(x => x.Id).ToArray();
            }
            if(recipe.Results.Contains(0)) {
                List<IdCount> keep = recipe.Results
                    .Select((id, i) => (id, recipe.ResultCounts[i]))
                    .Where(x => x.id != 0)
                    .ToList()
                ;
                recipe.Results = keep.Select(x => x.Id).ToArray();
                recipe.ResultCounts = keep.Select(x => x.Id).ToArray();
            }

            return WashHands();
        }

        public Chef OnSuccess(Action<Chef> onSuccess) {
            if(!IsFaulted) onSuccess(this);
            return this;
        }
        public Chef OnSuccess(Func<Chef, Chef> onSuccess) {
            if(!IsFaulted) return onSuccess(this);
            return this;
        }
        public Chef OnFail(Action<Chef> onFaulted) {
            if(IsFaulted) onFaulted(this);
            return this;
        }
        public Chef OnFail(Func<Chef, Chef> onFaulted) {
            if(IsFaulted) return onFaulted(this);
            return this;
        }

        #region Internal Actions

        Chef _load() {
            if(Recipe is null) return this;

            if(At < 0 || At > _ids.Length) {
                Id = 0;
                Count = 0;
                IsFaulted = true;
                return this;
            } else {
                Id = _ids[At];
                Count = _counts[At];

                IsFaulted = false;
                return this;
            }
        }

        Chef _save() {
            if(IsFaulted || !IsBound) {
                IsFaulted = true;
                return this;
            }

            _ids[At] = Id;
            _counts[At] = Count;

            return this;
        }

        Chef _setList(IdCount[] with) {
            if(Recipe is null) return this;
            IsFaulted = true;

            foreach(var id in _ids) {
                if(!Item._TryGetValue(id, out var item)) continue;
                if(IsInput) item.Proto.makes.Remove(this);
                else item.Proto.recipes.Remove(this);
            }

            if(_ids.Length == with.Length) {
                for(int i = with.Length; i-->0;) {
                    ref var on = ref with[i];
                    _ids[i] = on.Id;
                    _counts[i] = on.Count;
                }
            } else {
                if(IsInput) {
                    Recipe.Items = with.Select(x => x.Id).ToArray();
                    Recipe.ItemCounts = with.Select(x => x.Count).ToArray();
                } else {
                    Recipe.Results = with.Select(x => x.Id).ToArray();
                    Recipe.ResultCounts = with.Select(x => x.Count).ToArray();
                }
            }

            foreach(var ic in with) {
                if(!Item._TryGetValue(ic.Id, out var item)) continue;
                if(IsInput) item.Proto.makes.Add(this);
                else item.Proto.recipes.Add(this);
            }
            return this;
        }
        Chef _setList(ItemCount[] with) => _setList(with.Select(ic => (ic.Item.Id, ic.Count)).ToArray());

        Chef _addList(IdCount[] list) {
            if(Recipe is null) return this;

            IsFaulted = true;

            if(IsInput) {
                Recipe.Items = Recipe.Items.Concat(list.Select(x => x.Id)).ToArray();
                Recipe.ItemCounts = Recipe.Items.Concat(list.Select(x => x.Count)).ToArray();
            } else {
                Recipe.Results = Recipe.Results.Concat(list.Select(x => x.Id)).ToArray();
                Recipe.ResultCounts = Recipe.ResultCounts.Concat(list.Select(x => x.Count)).ToArray();
            }

            foreach(var ic in list) {
                if(!Item._TryGetValue(ic.Id, out var item)) continue;
                if(IsInput) item.Proto.makes.Add(this);
                else item.Proto.recipes.Add(this);
            }

            return this;
        }
        Chef _addList(ItemCount[] with) => _addList(with.Select(ic => (ic.Item.Id, ic.Count)).ToArray());

        Chef _setId(int id) { Id = id; return this; }
        Chef _setCount(int count) { Count = count; return this; }
        Chef _setCount(double count) { Count = count.TruncInt(); return this; }
        Chef _setAt(int at) { At = at; return this; }

        Chef _setCur(int id, int count) { Id = id; Count = count; return this; }
        Chef _setCur(int id, double count) { Id = id; Count = count.TruncInt(); return this; }
        Chef _setAll(int id, int count, int at) { Id = id; Count = count; At = at; return this; }

        Chef _useInputs() { IsInput = true; return this; }
        Chef _useResults() { IsResult = true; return this; }

        #endregion
        #region Internal recipe tracking helpers

        Chef _track() {
            if(IsFaulted || !IsBound) return this;

            if(IsInput) Item.Proto.makes.Add(this);
            else Item.Proto.recipes.Add(this);
            return this;
        }
        Chef _untrack() {
            if(IsFaulted || !IsBound) return this;

            if(IsInput) Item.Proto.makes.Remove(this);
            else Item.Proto.recipes.Remove(this);
            return this;
        }

        #endregion
        #region Load/Find Current
        public Chef Reload() => IsFaulted ? this : _load();

        public Chef LoadInput(int at) => _useInputs()._setAt(at)._load();
        public Chef LoadResult(int at) => _useResults()._setAt(at)._load();
        public Chef LoadOnlyResult() {
            _useResults();
            if(_ids.Length == 1) return LoadResult(0);

            IsFaulted = true;
            return this;
        }

        public Chef FindInput(int itemId) => _useInputs()._setAt(Array.IndexOf(_ids, itemId))._load();
        public Chef FindResult(int itemId) => _useResults()._setAt(Array.IndexOf(_ids, itemId))._load();

        #endregion
        #region Change Current

        public Chef SetQty(int to) => _setCount(to)._save();
        public Chef SetQty(Func<int, int> reducer) => _setCount(reducer(Count))._save();
        public Chef SetQty(Func<int, double> reducer) => _setCount(reducer(Count))._save();

        public Chef Substitute(int itemId) => _untrack()._setId(itemId)._save()._track();
        public Chef Substitute(int itemId, int qty) => _untrack()._setCur(itemId, qty)._save()._track();
        public Chef Substitute(int itemId, Func<int, int> qty) => _untrack()._setCur(itemId, qty(Count))._save()._track();
        public Chef Substitute(int itemId, Func<int, double> qty) => _untrack()._setCur(itemId, qty(Count))._save()._track();

        /// <summary>
        /// Marks for deletion. Requires a call to <c>CleanRecipe</c> to apply.
        /// </summary>
        public Chef Toss() => _untrack()._setId(0)._save();

        #endregion
        #region List Rewrites
        public Chef AddItem(int itemId, int qty) => AddItems((itemId, qty));
        public Chef AddResult(int itemId, int qty) => AddResults((itemId, qty));

        public Chef AddItems(params IdCount[] toAdd) => _useInputs()._addList(toAdd).WashHands();
        public Chef AddItems(params ItemCount[] toAdd) => _useInputs()._addList(toAdd).WashHands();
        public Chef AddResults(params IdCount[] toAdd) => _useResults()._addList(toAdd).WashHands();
        public Chef AddResults(params ItemCount[] toAdd) => _useResults()._addList(toAdd).WashHands();

        public Chef OverwriteAllItems(params IdCount[] with) => _useInputs()._setList(with).WashHands();
        public Chef OverwriteAllItems(params ItemCount[] with) => _useInputs()._setList(with).WashHands();
        public Chef OverwriteAllResults(params IdCount[] with) => _useResults()._setList(with).WashHands();
        public Chef OverwriteAllResults(params ItemCount[] with) => _useResults()._setList(with).WashHands();

        public Chef CopyItems(RecipeProto from) {
            if(Recipe is null) return this;
            IsFaulted = true;

            foreach(var id in Recipe.Items) {
                if(!Item._TryGetValue(id, out var item)) continue;
                item.Proto.makes.Remove(this);
            }

            Recipe.Items = from.Items.ToArray();
            Recipe.ItemCounts = from.ItemCounts.ToArray();

            foreach(var id in Recipe.Items) {
                if(!Item._TryGetValue(id, out var item)) continue;
                item.Proto.makes.Add(this);
            }

            return WashHands();
        }
        public Chef CopyResults(RecipeProto from) {
            if(Recipe is null) return this;
            IsFaulted = true;

            foreach(var id in Recipe.Results) {
                if(!Item._TryGetValue(id, out var item)) continue;
                item.Proto.recipes.Remove(this);
            }

            Recipe.Results = from.Results.ToArray();
            Recipe.ResultCounts = from.ResultCounts.ToArray();

            foreach(var id in Recipe.Results) {
                if(!Item._TryGetValue(id, out var item)) continue;
                item.Proto.recipes.Add(this);
            }

            return WashHands();
        }

        #endregion

        #region Recipe Action Helpers

        public readonly Chef Rename(string to) {
            if(IsFaulted) return this;
            Recipe.Name = Recipe.name = to; return this;
        }
        public readonly Chef Rename(RecipeProto like) {
            if(IsFaulted) return this;
            Recipe.Name = like.Name;
            Recipe.name = like.name;
            return this;
        }

        public readonly Chef Describe(string desc) {
            if(IsFaulted) return this;
            Recipe.Description = Recipe.description = desc; return this;
        }
        public readonly Chef Describe(RecipeProto like) {
            if(IsFaulted) return this;
            Recipe.Description = like.Description;
            Recipe.description = like.description;
            return this;
        }

        public readonly Chef TechRequired(TechProto tech) {
            if(IsFaulted) return this;
            Recipe.preTech = tech;
            return this;
        }
        public readonly Chef TechRequired(RecipeProto like) => TechRequired(like.preTech);

        public readonly Chef SetProductive(bool to) {
            if(IsFaulted) return this;
            Recipe.productive = to;
            return this;
        }
        public readonly Chef SetProductive(RecipeProto like) => SetProductive(like.productive);

        public readonly Chef SetType(ERecipeType to) {
            if(IsFaulted) return this;
            Recipe.Type = to;
            return this;
        }
        public readonly Chef SetType(RecipeProto like) => SetType(like.Type);

        public readonly Chef SetTime(int to) {
            if(IsFaulted) return this;
            Recipe.TimeSpend = to;
            return this;
        }
        public readonly Chef SetTime(RecipeProto like) => SetTime(like.TimeSpend);
        public readonly Chef SetTime(Func<int, int> reducer) => SetTime(reducer(Recipe.TimeSpend));
        public readonly Chef SetTime(Func<int, double> reducer) => SetTime(reducer(Recipe.TimeSpend).TruncInt());
        public readonly Chef SetTime(RecipeProto like, Func<int, int> reducer) => SetTime(reducer(like.TimeSpend));
        public readonly Chef SetTime(RecipeProto like, Func<int, double> reducer) => SetTime(reducer(like.TimeSpend).TruncInt());

        #endregion

        //Todo: include a helper for adding *new* recipes
    }

    public static partial class ChefExtensions {
        public static Chef StartCooking(this RecipeProto recipe) => (Chef)recipe;

        public static Chef CookFirstMadeBy(this IEnumerable<RecipeProto> recipes, int itemId) {
            return (Chef)recipes.FirstOrDefault(r => r.Items.Contains(itemId));
        }
        public static Chef CookFirstMaking(this IEnumerable<RecipeProto> recipes, int itemId) {
            return (Chef)recipes.FirstOrDefault(r => r.Results.Contains(itemId));
        }

        public static Chef CookFirst(this IEnumerable<RecipeProto> recipes, Func<RecipeProto, bool> predicate) {
            return (Chef)recipes.FirstOrDefault(predicate);
        }

        public static IEnumerable<IdCount> WalkItems(this RecipeProto recipe) {
            for(int i = 0; i < recipe.Results.Length; i++) {
                yield return (recipe.Items[i], recipe.ItemCounts[i]);
            }
        }
        public static IEnumerable<IdCount> WalkResults(this RecipeProto recipe) {
            for(int i = 0; i < recipe.Results.Length; i++) {
                yield return (recipe.Results[i], recipe.ResultCounts[i]);
            }
        }


        public static int TruncInt(this double dbl) => (int)Math.Truncate(dbl);
    }
}
