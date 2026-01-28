import { useAppDispatch } from "#/lib/store/hooks";
import { storeAdminAccessKey } from "#/lib/store/ollamaflow/reducer";
import { logout } from "#/lib/store/rootReducer";

export const useCredentialsToLogin = () => {
  const dispatch = useAppDispatch();
  const loginWithCredentials = (accessKey: string) => {
    dispatch(storeAdminAccessKey(accessKey));
  };
  return loginWithCredentials;
};

export const useAdminCredentialsToLogin = () => {
  const dispatch = useAppDispatch();
  const loginWithAdminCredentials = (accessKey: string) => {
    dispatch(storeAdminAccessKey(accessKey));
  };
  return loginWithAdminCredentials;
};

export const useLogout = () => {
  const dispatch = useAppDispatch();

  const logOutFromSystem = () => {
    dispatch(logout());
  };
  return logOutFromSystem;
};
