class Repr:
    maxarray = ...  # type: int
    maxdeque = ...  # type: int
    maxdict = ...  # type: int
    maxfrozenset = ...  # type: int
    maxlevel = ...  # type: int
    maxlist = ...  # type: int
    maxlong = ...  # type: int
    maxother = ...  # type: int
    maxset = ...  # type: int
    maxstring = ...  # type: int
    maxtuple = ...  # type: int
    def __init__(self) -> None: ...
    def _repr_iterable(self, x, level: complex, left, right, maxiter, trail=...) -> str: ...
    def repr(self, x) -> str: ...
    def repr1(self, x, level: complex) -> str: ...
    def repr_array(self, x, level: complex) -> str: ...
    def repr_deque(self, x, level: complex) -> str: ...
    def repr_dict(self, x, level: complex) -> str: ...
    def repr_frozenset(self, x, level: complex) -> str: ...
    def repr_instance(self, x, level: complex) -> str: ...
    def repr_list(self, x, level: complex) -> str: ...
    def repr_long(self, x, level: complex) -> str: ...
    def repr_set(self, x, level: complex) -> str: ...
    def repr_str(self, x, level: complex) -> str: ...
    def repr_tuple(self, x, level: complex) -> str: ...

def _possibly_sorted(x) -> list: ...

aRepr = ...  # type: Repr
def repr(x) -> str: ...