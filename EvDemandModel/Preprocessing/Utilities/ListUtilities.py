class ListUtilities:

    @staticmethod
    def intersection_of_lists(*args):
        
        # If no lists are provided, return an empty list
        if not args:
            return []

        # Start with the set of the first list
        result_set = set(args[0])

        # Iterate over the remaining lists, updating the result_set
        for lst in args[1:]:
            result_set &= set(lst)

        return list(result_set)