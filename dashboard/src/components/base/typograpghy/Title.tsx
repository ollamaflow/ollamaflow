import { Typography } from 'antd';
import { TitleProps } from 'antd/es/typography/Title';
import styles from './typography.module.css';
import classNames from 'classnames';

const { Title } = Typography;

export type OllamaFlowTitleProps = TitleProps & {
  weight?: number;
  fontSize?: number;
};

const OllamaFlowTitle = (props: OllamaFlowTitleProps) => {
  const { children, className, style, color, weight, fontSize, ...rest } = props;
  return (
    <Title
      className={classNames(styles.headingCommonStyles, className)}
      style={{ color: color, fontWeight: weight, fontSize: fontSize, ...style }}
      {...rest}
    >
      {children}
    </Title>
  );
};

export default OllamaFlowTitle;
