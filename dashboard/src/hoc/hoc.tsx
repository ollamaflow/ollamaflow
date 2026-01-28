import PageLoading from "#/components/base/loading/PageLoading";
import LogoutFallBack from "#/components/logout-fallback/LogoutFallBack";
import { paths } from "#/constants/constant";

import { useAppSelector } from "#/lib/store/hooks";
import { RootState } from "#/lib/store/store";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export const withAuth = (WrappedComponent: React.ElementType) => {
  const WithAuth = (props: any) => {
    const adminAccessKey = useAppSelector(
      (state: RootState) => state.ollamaFlow.adminAccessKey
    );
    const [hasValidAuth, setHasValidAuth] = useState<boolean | null>(null);

    useEffect(() => {
      if (adminAccessKey) {
        setHasValidAuth(true);
      } else {
        setHasValidAuth(false);
      }
      //eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return hasValidAuth === null ? (
      <PageLoading className="h-100vh" />
    ) : hasValidAuth ? (
      <WrappedComponent {...props} />
    ) : (
      <LogoutFallBack logoutPath={paths.Login} />
    );
  };
  return WithAuth;
};

export const forGuest = (WrappedComponent: React.ElementType) => {
  const ForGuest = (props: any) => {
    const adminAccessKey = useAppSelector(
      (state: RootState) => state.ollamaFlow.adminAccessKey
    );
    const router = useRouter();
    useEffect(() => {
      if (adminAccessKey) {
        router.push(paths.Dashboard);
      }
      //eslint-disable-next-line react-hooks/exhaustive-deps
    }, [adminAccessKey]);

    return !adminAccessKey ? <WrappedComponent {...props} /> : null;
  };
  return ForGuest;
};
