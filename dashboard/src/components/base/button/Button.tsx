import { Button, ButtonProps } from 'antd';

interface OllamaFlowButtonProps extends ButtonProps {
  weight?: number;
}

const OllamaFlowButton = (props: OllamaFlowButtonProps) => {
  const { weight, icon, ...rest } = props;
  return <Button {...rest} icon={icon} style={{ fontWeight: weight }} />;
};

export default OllamaFlowButton;
