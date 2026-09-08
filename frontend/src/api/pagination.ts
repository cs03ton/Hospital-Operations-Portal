export type PagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type PaginationQuery = {
  page?: number;
  pageSize?: number;
};
