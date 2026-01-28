import { createContext, useContext, useState } from 'react';
import { ThemeEnum } from '../types/types';

export const usePagination = () => {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [skip, setSkip] = useState(0);

  const handlePageChange = (page: number, pageSize: number) => {
    setPage(page);
    setPageSize(pageSize);
    setSkip((page - 1) * pageSize);
  };

  return { page, pageSize, skip, handlePageChange };
};

export const AppContext = createContext({
  theme: ThemeEnum.LIGHT,
  setTheme: (theme: ThemeEnum) => {},
});

export const useAppContext = () => {
  return useContext(AppContext);
};
