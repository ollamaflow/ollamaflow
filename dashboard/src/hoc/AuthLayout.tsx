import { localStorageKeys } from "#/constants/constant";
import { useAppDispatch } from "#/lib/store/hooks";
import { storeAdminAccessKey } from "#/lib/store/ollamaflow/reducer";
import React, { useEffect, useState } from "react";
import PageLoading from "#/components/base/loading/PageLoading";
import {
  changeAxiosBaseUrl,
  setAuthToken,
} from "#/lib/store/rtk/rtkApiInstance";
export const initializeAuthFromLocalStorage = () => {
  const auth: any = {};
  if (typeof window !== "undefined") {
    const serverUrl = localStorage.getItem(localStorageKeys.serverUrl);

    const adminAccessKey = localStorage.getItem(
      localStorageKeys.adminAccessKey
    );
    if (serverUrl) {
      auth.serverUrl = serverUrl;
    }
    if (adminAccessKey) {
      auth.adminAccessKey = adminAccessKey;
    }
    return auth;
  }
  return null;
};

const AuthLayout = ({
  children,
  className,
}: Readonly<{ children: React.ReactNode; className?: string }>) => {
  const [isReady, setIsReady] = useState(false);
  const dispatch = useAppDispatch();
  const localStorageAuth = initializeAuthFromLocalStorage();

  useEffect(() => {
    if (localStorageAuth?.adminAccessKey && localStorageAuth?.serverUrl) {
      dispatch(storeAdminAccessKey(localStorageAuth.adminAccessKey));
      changeAxiosBaseUrl(localStorageAuth.serverUrl);
      setAuthToken(localStorageAuth.adminAccessKey);
    }
    setIsReady(true);
    //eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (!isReady) {
    return <PageLoading />;
  }
  return (
    <div id="root-div" className={className}>
      {children}
    </div>
  );
};

export default AuthLayout;
